namespace PMTiles.Sources;

/// <summary>
/// Represents a PMTiles source.
/// </summary>
public abstract class Source : IDisposable, IAsyncDisposable
{
    private const int HeaderSize = 127;

    private Header? _header;
    
    private Dictionary<MemoryPosition, TileEntry[]> _cache = new();
    
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        Dispose(disposing: true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache.Clear();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await DisposeAsyncCore();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }
    
    protected virtual ValueTask DisposeAsyncCore()
    {
        return default;
    }

    /// <summary>
    /// Gets the header and root directory and caches it.
    /// </summary>
    /// <param name="etag">The ETag to use for caching.</param>
    /// <returns>A tuple containing the header and root directory.</returns>
    public async ValueTask<(Header, TileEntry[])> GetHeaderAndRoot(string? etag = null)
    {
        if (_header is not null)
        {
            var root = new MemoryPosition(_header.RootDirectoryOffset, _header.RootDirectoryLength);
            if (_header.Etag == etag && _cache.TryGetValue(root, out var entries))
            {
                return (_header, entries);
            }
        }
        var buffer = await GetTileDataAsync(new MemoryPosition(0, 16384));  // Header + RootDir

        var headerData = buffer[..HeaderSize];
        var header = BytesToHeader(headerData, etag);
        
        _header = header;
        
        // Read RootDir
        var rootDirectoryData = buffer[(int)header.RootDirectoryOffset..(int)(header.RootDirectoryOffset + header.RootDirectoryLength)];
        var rootDirectory = await BytesToDirectory(rootDirectoryData, header);
        _cache[new MemoryPosition(header.RootDirectoryOffset, header.RootDirectoryLength)] = rootDirectory;
        return (header, rootDirectory);
    }
    
    public Header GetHeaderSync(string? etag = null)
    {
        if (_header is not null)
        {
            return _header;
        }
        var buffer = GetTileData(new MemoryPosition(0, 16384));  // Header + RootDir

        if (buffer.Length < HeaderSize)
        {
            throw new Exception("Cannot read header");
        }
        
        var headerData = buffer[..HeaderSize];
        var header = BytesToHeader(headerData, etag);
        
        return _header = header;
    }

    protected abstract Task<Memory<byte>> GetTileDataAsync(MemoryPosition position);
    
    protected abstract Memory<byte> GetTileData(MemoryPosition position);

    private static Header BytesToHeader(Memory<byte> buffer, string? etag = null)
    {
        using var stream = PMTilesHelper.CreateBinaryReader(buffer);
        using var reader = new BinaryReader(stream);
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        if (reader.ReadUInt16() != 0x4D50) // "PM"
        {
            throw new Exception("Wrong magic number");
        }
        reader.BaseStream.Seek(7, SeekOrigin.Begin);
        var specVersion = reader.ReadByte(); // uint8
        if (specVersion > 3)
        {
            throw new Exception($"Archive is spec version {specVersion} but this library only supports up to 3");
        }

        return new Header
        {
            SpecVersion = specVersion,
            RootDirectoryOffset = reader.ReadUInt64(), // at 8
            RootDirectoryLength = reader.ReadUInt64(), // at 16
            JsonMetadataOffset = reader.ReadUInt64(), // at 24
            JsonMetadataLength = reader.ReadUInt64(), // at 32
            LeafDirectoryOffset = reader.ReadUInt64(), // at 40
            LeafDirectoryLength = reader.ReadUInt64(), // at 48
            TileDataOffset = reader.ReadUInt64(), // at 56
            TileDataLength = reader.ReadUInt64(), // at 64
            NumAddressedTiles = reader.ReadUInt64(), // at 72
            NumTileEntries = reader.ReadUInt64(), // at 80
            NumTileContents = reader.ReadUInt64(), // at 88
            Clustered = reader.ReadByte() == 1, // at 96
            InternalCompression = (Compression)reader.ReadByte(), // at 97
            TileCompression = (Compression)reader.ReadByte(), // at 98
            TileType = (TileType)reader.ReadByte(), // at 99
            MinZoom = reader.ReadByte(), // at 100
            MaxZoom = reader.ReadByte(), // at 101
            MinLon = reader.ReadInt32() / 1e7, // at 102
            MinLat = reader.ReadInt32() / 1e7, // at 106
            MaxLon = reader.ReadInt32() / 1e7, // at 110
            MaxLat = reader.ReadInt32() / 1e7, // at 114
            CenterZoom = reader.ReadByte(), // at 118
            CenterLon = reader.ReadInt32() / 1e7, // at 119
            CenterLat = reader.ReadInt32() / 1e7, // at 123
            Etag = etag
        };
    }

    private static async Task<TileEntry[]> BytesToDirectory(Memory<byte> buffer, Header header)
    {
        using var memoryStream = PMTilesHelper.CreateBinaryReader(buffer);
        await using var stream = PMTilesHelper.Decompress(memoryStream, header.InternalCompression);
        var entries = stream.ReadVarint();
        var tileEntries = new TileEntry[entries];
        
        ulong lastId = 0;
        for (ulong i = 0; i < entries; i++)
        {
            var value = stream.ReadVarint();
            lastId += value;
            tileEntries[i] = new TileEntry
            {
                TileId = lastId,
                Offset = 0,
                Length = 0
            };
        }

        for (ulong i = 0; i < entries; i++)
        {
            tileEntries[i].RunLength = stream.ReadVarint();
        }
        for (ulong i = 0; i < entries; i++)
        {
            tileEntries[i].Length = stream.ReadVarint();
        }
        
        for (ulong i = 0; i < entries; i++) {
            var value = stream.ReadVarint();
            if (value == 0 && i > 0) {
                tileEntries[i].Offset = tileEntries[i - 1].Offset + tileEntries[i - 1].Length;
            } else {
                tileEntries[i].Offset = value - 1;
            }
        }

        return tileEntries;
    }

    private async ValueTask<TileEntry[]> GetTileEntries(MemoryPosition position)
    {
        if (_header is null)
        {
            throw new InvalidOperationException("Header is not set");
        }
        if (_cache.TryGetValue(position, out var entries))
        {
            return entries;
        }
        
        var buffer = await GetTileDataAsync(position);
        var tileEntries = await BytesToDirectory(buffer, _header);
        _cache[position] = tileEntries;
        return tileEntries;
    }

    private static TileEntry? FindTile(TileEntry[] entries, long tileId) {
        var m = 0L;
        var n = entries.LongLength - 1;
        while (m <= n) {
            var k = (n + m) >> 1;
            var cmp = tileId - (long)entries[k].TileId;
            switch (cmp)
            {
                case > 0:
                    m = k + 1;
                    break;
                case < 0:
                    n = k - 1;
                    break;
                default:
                    return entries[k];
            }
        }

        // when m > n
        if (n < 0) return null;
        if (entries[n].RunLength == 0) {
            return entries[n];
        }

        return tileId - (long)entries[n].TileId >= (long)entries[n].RunLength ? null : entries[n];
    }
    
    public async Task<Stream?> GetTile(long tileId) {
        if (_header is null) {
            _ = await GetHeaderAndRoot(); // cache header
        }
        var pos = new MemoryPosition(_header!.RootDirectoryOffset, _header.RootDirectoryLength);
        for (var i = 0; i < 3; i++)
        {
            var entries = await GetTileEntries(pos);
            var entry = FindTile(entries, tileId);
            if (entry is null) {
                return null;
            }

            if (entry.RunLength > 0)
            {
                pos = new MemoryPosition(_header.TileDataOffset + entry.Offset, entry.Length);
                var mem = await GetTileDataAsync(pos);
                var decompress = PMTilesHelper.Decompress(PMTilesHelper.CreateBinaryReader(mem), _header.TileCompression);
                return decompress;
            }
            
            // if RunLength is 0, it means the entry describes the leaf directory we need to read next
            pos = new MemoryPosition(_header.LeafDirectoryOffset + entry.Offset, entry.Length);
        }
        // not found
        return null;
    }
}