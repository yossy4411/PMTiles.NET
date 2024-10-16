namespace PMTiles.Sources;

public class CachedSource(Stream stream) : Source(stream), IDisposable, IAsyncDisposable
{
    public const int HeaderSize = 127;
    
    private Header? _header;
    private TileEntry[]? _rootDirectory;
    
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
        if (_header != null && _rootDirectory != null)
        {
            return (_header, _rootDirectory);
        }
        var buffer = new Memory<byte>(new byte[16384], 0, 16384);
        var read = await Stream.ReadAsync(buffer);
        if (read == 0)
        {
            throw new Exception("Failed to read header");
        }
        
        var headerData = buffer[..HeaderSize];
        var header = BytesToHeader(headerData, etag);
        
        // Read RootDir
        var rootDirectoryData = buffer[(int)header.RootDirectoryOffset..(int)(header.RootDirectoryOffset + header.RootDirectoryLength)];
        var rootDirectory = await RootDirectory(rootDirectoryData, header);
        
        _header = header;
        _rootDirectory = rootDirectory;
        
        return (header, _rootDirectory);
    }

    internal override Header BytesToHeader(Memory<byte> buffer, string? etag = null)
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
    
    private static async Task<TileEntry[]> RootDirectory(Memory<byte> buffer, Header header)
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