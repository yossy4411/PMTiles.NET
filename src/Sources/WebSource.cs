using System.Net.Http.Headers;

namespace PMTiles.Sources;

public class WebSource : Source, IDisposable
{
    private HttpClient Client { get; } = new();
    
    private readonly string _url;
    
    public WebSource(string url)
    {
        _url = url;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Client.Dispose();
        }
        base.Dispose(disposing);
    }
    
    protected override ValueTask DisposeAsyncCore()
    {
        Client.Dispose();
        return base.DisposeAsyncCore();
    }

    public async Task<bool> IsAvailable()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, _url);
        var response = await Client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
    
    protected override async Task<Memory<byte>> GetTileData(MemoryPosition position)
    {
        Client.DefaultRequestHeaders.Range = new RangeHeaderValue((long)position.Offset, (long)(position.Offset + position.Length - 1));
        var buffer = await Client.GetByteArrayAsync(_url);
        return buffer;
    }
    
    
}