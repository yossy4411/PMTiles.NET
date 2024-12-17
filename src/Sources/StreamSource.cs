namespace PMTiles.Sources;

/// <summary>
/// A source that fetches tiles from a stream
/// </summary>
public class StreamSource : Source
{
    private Stream Stream { get; }
    
    /// <summary>
    /// A source that fetches tiles from a stream
    /// </summary>
    /// <param name="stream">Source Stream</param>
    public StreamSource(Stream stream)
    {
        Stream = stream;
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stream.Dispose();
        }
        base.Dispose(disposing);
    }
    
    protected override async ValueTask DisposeAsyncCore()
    {
        await Stream.DisposeAsync();
    }

    protected override async Task<Memory<byte>> GetTileDataAsync(MemoryPosition position)
    {
        var buffer = new Memory<byte>(new byte[position.Length]);
        Stream.Seek((long)position.Offset, SeekOrigin.Begin);
        var read = await Stream.ReadAsync(buffer);
        if (read == 0)
        {
            throw new Exception("Failed to read tile data");
        }
        return buffer;
    }

    protected override Memory<byte> GetTileData(MemoryPosition position)
    {
        var buffer = new byte[position.Length];
        Stream.Seek((long)position.Offset, SeekOrigin.Begin);
        var read = Stream.Read(buffer);
        if (read == 0)
        {
            throw new Exception("Failed to read tile data");
        }
        return buffer;
    }
}