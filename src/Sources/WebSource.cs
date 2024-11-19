﻿using System.Net.Http.Headers;

namespace PMTiles.Sources;

public class WebSource : Source
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

    public async Task<bool> IsAvailableAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, _url);
        var response = await Client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
    
    public bool IsAvailable()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, _url);
        var response = Client.Send(request);
        return response.IsSuccessStatusCode;
    }
    
    protected override async Task<Memory<byte>> GetTileDataAsync(MemoryPosition position)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        request.Headers.Range = new RangeHeaderValue((long)position.Offset, (long)(position.Offset + position.Length - 1));
        
        var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to get tile data. Status code: {response.StatusCode}");
        }
        return await response.Content.ReadAsByteArrayAsync();
    }

    protected override Memory<byte> GetTileData(MemoryPosition position)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        request.Headers.Range = new RangeHeaderValue((long)position.Offset, (long)(position.Offset + position.Length - 1));
        
        var response = Client.Send(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to get tile data. Status code: {response.StatusCode}");
        }

        var stream = response.Content.ReadAsStream();
        var buffer = new byte[position.Length];
        var read = stream.Read(buffer, 0, buffer.Length);
        if (read != buffer.Length)
        {
            throw new Exception("Failed to read tile data");
        }
        return buffer;
    }
}