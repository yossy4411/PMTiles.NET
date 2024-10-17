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
    public ulong NumTileEntries { get; init; }
    public ulong NumTileContents { get; init; }
    internal bool Clustered { get; init; }
    public Compression InternalCompression { get; init; }
    public Compression TileCompression { get; init; }
    public TileType TileType { get; init; }
    public byte MinZoom { get; init; }
    public byte MaxZoom { get; init; }
    public double MinLon { get; init; }
    public double MinLat { get; init; }
    public double MaxLon { get; init; }
    public double MaxLat { get; init; }
    public byte CenterZoom { get; init; }
    public double CenterLon { get; init; }
    public double CenterLat { get; init; }
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