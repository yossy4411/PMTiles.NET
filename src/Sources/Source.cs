namespace PMTiles.Sources;

/// <summary>
/// Represents a PMTiles source.
/// </summary>
/// <param name="stream">The stream to read from.</param>
public abstract class Source(Stream stream)
{
    protected const int HeaderSize = 127;
    protected Stream Stream { get; } = stream;
    
    protected Header? Header;
    
    /// <summary>
    /// Gets the header and root directory.
    /// </summary>
    /// <param name="etag">The ETag to use for caching.</param>
    /// <returns>A tuple containing the header and root directory.</returns>
    public abstract Task<(Header, TileEntry[])> GetHeaderAndRoot(string? etag = null);


    private protected static async Task<Memory<byte>> GetTileData(Stream stream, MemoryPosition position)
    {
        var buffer = new Memory<byte>(new byte[position.Length]);
        stream.Seek((long)position.Offset, SeekOrigin.Begin);
        var read = await stream.ReadAsync(buffer);
        if (read == 0)
        {
            throw new Exception("Failed to read tile data");
        }
        return buffer;
    }
    
    private protected abstract Header BytesToHeader(Memory<byte> buffer, string? etag = null);
    
    private protected abstract Task<TileEntry[]> BytesToDirectory(Memory<byte> buffer, Header header);
    
    protected abstract Task<TileEntry[]> GetTileEntries(MemoryPosition position);

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
    
    public async Task<byte[]?> GetTile(long tileId) {
        if (Header is null) {
            throw new InvalidOperationException("Header is not set");
        }
        var pos = new MemoryPosition(Header.RootDirectoryOffset, Header.RootDirectoryLength);
        for (var i = 0; i < 3; i++)
        {
            var entries = await GetTileEntries(pos);
            var entry = FindTile(entries, tileId);
            if (entry is null) {
                return null;
            }

            if (entry.RunLength > 0)
            {
                pos = new MemoryPosition(Header.TileDataOffset + entry.Offset, entry.Length);
                var mem = await GetTileData(Stream, pos);
                await using var decompress = PMTilesHelper.Decompress(PMTilesHelper.CreateBinaryReader(mem), Header.TileCompression);
                using var memStream = new MemoryStream();
                await decompress.CopyToAsync(memStream);
                var data = memStream.ToArray();
                return data;
            }
            
            // if RunLength is 0, it means the entry describes the leaf directory we need to read next
            pos = new MemoryPosition(Header.LeafDirectoryOffset + entry.Offset, entry.Length);
        }
        // not found
        return null;
    }
}