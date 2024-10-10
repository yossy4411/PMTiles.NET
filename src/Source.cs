using System.Reflection.Metadata.Ecma335;

namespace PMTiles.NET;

public class Source
{
    public Source(Stream stream)
    {
        Stream = stream;
    }

    public Source(string path)
    {
        Stream = File.OpenRead(path);
    }

    public Stream Stream { get; }
    
    public async Task<Header> GetHeader()
    {
    }
}

/// <summary>
/// PMTiles header
/// </summary>
public class Header
{
    public float SpecVersion { get; init; }
    internal uint RootDirectoryOffset { get; init; }
    internal uint RootDirectoryLength { get; init; }
    internal uint JsonMetadataOffset { get; init; }
    internal uint JsonMetadataLength { get; init; }
    internal uint LeafDirectoryOffset { get; init; }
    internal uint LeafDirectoryLength { get; init; }
    internal uint TileDataOffset { get; init; }
    internal uint TileDataLength { get; init; }
    internal uint NumAddressedTiles { get; init; }
    internal uint NumTileEntries { get; init; }
    internal uint NumTileContents { get; init; }
    internal bool Clustered { get; init; }
    internal Compression InternalCompression { get; init; }
    internal Compression TileCompression { get; init; }
    internal TileType TileType { get; init; }
    internal uint MinZoom { get; init; }
    internal uint MaxZoom { get; init; }
    internal double MinLon { get; init; }
    internal double MinLat { get; init; }
    internal double MaxLon { get; init; }
    internal double MaxLat { get; init; }
    internal uint CenterZoom { get; init; }
    internal double CenterLon { get; init; }
    internal double CenterLat { get; init; }
    internal string? Etag { get; init; }
}

/// <summary>
/// Compression type
/// </summary>
public enum Compression
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
public enum TileType
{
    Unknown,
    Mvt, // Mapbox Vector Tiles
    Png,
    Jpeg,
    Webp,
    Avif,
}