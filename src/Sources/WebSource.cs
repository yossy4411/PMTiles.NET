using System.Net.Http.Headers;

namespace PMTiles.Sources;

public class WebSource : Source, IDisposable
{
    private HttpClient Client { get; } = new();
    
    public bool IsDisposed { get; private set; }
    
    private string _url;
    
    public WebSource(string url)
    {
        _url = url;
    }
    
    public void Dispose()
    {
        Client.Dispose();
        IsDisposed = true;
        GC.SuppressFinalize(this);
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