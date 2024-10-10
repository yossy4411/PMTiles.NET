using System.Reflection.Metadata.Ecma335;

namespace PMTiles.NET;

public class Source : IDisposable, IAsyncDisposable
{
    public Stream Stream { get; }
    
    public Source(Stream stream)
    {
        Stream = stream;
    }
    
    public async Task<Header> GetHeader(string? etag = null)
    {
        // Read header (0-16384 bytes)
        var buffer = new byte[16384];
        var bytesRead = 0;
        while (bytesRead < 16384)
        {
            var read = await Stream.ReadAsync(buffer.AsMemory(bytesRead, 16384 - bytesRead));
            if (read == 0)
            {
                throw new Exception("Failed to read header");
            }
            bytesRead += read;
        }

        var headerData = buffer[..PMTilesHelper.HeaderSize];
        return BytesToHeader(etag, headerData);
    }

    private static Header BytesToHeader(string? etag, byte[] headerData)
    {
        using var stream = new MemoryStream(headerData);
        using var reader = new BinaryReader(stream);
        stream.Seek(0, SeekOrigin.Begin);
        if (reader.ReadUInt16() != 0x4D50) // "PM"
        {
            throw new Exception("Wrong magic number");
        }
        stream.Seek(7, SeekOrigin.Begin);
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
}

/// <summary>
/// PMTiles header
/// </summary>
public class Header
{
    public byte SpecVersion { get; init; }
    internal ulong RootDirectoryOffset { get; init; }
    internal ulong RootDirectoryLength { get; init; }
    internal ulong JsonMetadataOffset { get; init; }
    internal ulong JsonMetadataLength { get; init; }
    internal ulong LeafDirectoryOffset { get; init; }
    internal ulong LeafDirectoryLength { get; init; }
    internal ulong TileDataOffset { get; init; }
    internal ulong TileDataLength { get; init; }
    internal ulong NumAddressedTiles { get; init; }
    internal ulong NumTileEntries { get; init; }
    internal ulong NumTileContents { get; init; }
    internal bool Clustered { get; init; }
    internal Compression InternalCompression { get; init; }
    internal Compression TileCompression { get; init; }
    internal TileType TileType { get; init; }
    internal byte MinZoom { get; init; }
    internal byte MaxZoom { get; init; }
    internal double MinLon { get; init; }
    internal double MinLat { get; init; }
    internal double MaxLon { get; init; }
    internal double MaxLat { get; init; }
    internal byte CenterZoom { get; init; }
    internal double CenterLon { get; init; }
    internal double CenterLat { get; init; }
    internal string? Etag { get; init; }
}

/// <summary>
/// Compression type
/// </summary>
public enum Compression : byte
{
    Unknown,
    None,
    Gzip,
    Brotli,
    Zstd
}

/// <summary>
/// Tile type
/// </summary>
public enum TileType : byte
{
    Unknown,
    Mvt, // Mapbox Vector Tiles
    Png,
    Jpeg,
    Webp,
    Avif,
}