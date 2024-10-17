namespace PMTiles.Sources;

public class CachedSource(Stream stream) : Source(stream), IDisposable, IAsyncDisposable
{
    private Dictionary<MemoryPosition, TileEntry[]> _cache = new();
    
    public bool IsDisposed { get; private set; }
    
    public void Dispose()
    {
        Stream.Dispose();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    public override async Task<(Header, TileEntry[])> GetHeaderAndRoot(string? etag = null)
    {
        // Read header (0-16384 bytes)
        var memPos = new MemoryPosition(0, 16384);
        if (_cache.TryGetValue(memPos, out var entries) && Header is not null)
        {
            return (Header, entries);
        }
        var buffer = await GetTileData(Stream, memPos);

        var headerData = buffer[..HeaderSize];
        var header = BytesToHeader(headerData, etag);
        
        Header = header;
        
        // Read RootDir
        var rootDirectoryData = buffer[(int)header.RootDirectoryOffset..(int)(header.RootDirectoryOffset + header.RootDirectoryLength)];
        var rootDirectory = await BytesToDirectory(rootDirectoryData, header);
        _cache[new MemoryPosition(header.RootDirectoryOffset, header.RootDirectoryLength)] = rootDirectory;
        return (header, rootDirectory);
    }

    protected override async Task<TileEntry[]> GetTileEntries(MemoryPosition position)
    {
        if (Header is null)
        {
            throw new InvalidOperationException("Header is not set");
        }
        if (_cache.TryGetValue(position, out var entries))
        {
            return entries;
        }
        
        var buffer = await GetTileData(Stream, position);
        var tileEntries = await BytesToDirectory(buffer, Header);
        _cache[position] = tileEntries;
        return tileEntries;
    }

    private protected override Header BytesToHeader(Memory<byte> buffer, string? etag = null)
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
    
    private protected override async Task<TileEntry[]> BytesToDirectory(Memory<byte> buffer, Header header)
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
}