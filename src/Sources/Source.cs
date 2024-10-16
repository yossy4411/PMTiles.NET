namespace PMTiles.Sources;

public abstract class Source(Stream stream)
{
    public Stream Stream { get; } = stream;

    public abstract Task<(Header, TileEntry[])> GetHeaderAndRoot(string? etag = null);
    internal abstract Header BytesToHeader(Memory<byte> buffer, string? etag = null);
    
    protected static TileEntry? FindTile(TileEntry[] entries, long tileId) {
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
}