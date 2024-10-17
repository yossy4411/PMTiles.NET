namespace PMTiles.Sources;

public class StreamSource(Stream stream) : Source, IDisposable, IAsyncDisposable
{
    public bool IsDisposed { get; private set; }
    
    private Stream Stream { get; }
    
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

    protected override async Task<Memory<byte>> GetTileData(MemoryPosition position)
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
}