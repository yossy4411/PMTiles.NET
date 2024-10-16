namespace PMTiles;

public class TileEntry
{
    public ulong TileId { get; init; }
    public ulong Offset { get; internal set; }
    public ulong Length { get; internal set; }
    public ulong RunLength { get; internal set; }
}