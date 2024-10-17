using System.Net.Http.Headers;

namespace PMTiles.Sources;

public class WebSource(string url) : Source, IDisposable
{
    private HttpClient Client { get; } = new();
    
    public bool IsDisposed { get; private set; }
    
    public void Dispose()
    {
        Client.Dispose();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    
    public async Task<bool> IsAvailable()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await Client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
    
    protected override async Task<Memory<byte>> GetTileData(MemoryPosition position)
    {
        Client.DefaultRequestHeaders.Range = new RangeHeaderValue((long)position.Offset, (long)(position.Offset + position.Length - 1));
        var buffer = await Client.GetByteArrayAsync(url);
        return buffer;
    }
    
    
}