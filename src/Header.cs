namespace PMTiles;

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