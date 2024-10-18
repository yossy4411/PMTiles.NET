using PMTiles.Sources;

namespace PMTiles;

public class PMTilesReader : IDisposable, IAsyncDisposable
{
    private Source? Source { get; set; }
    
    public PMTilesReader(Source source)
    {
        Source = source;
    }
    
    public void Dispose()
    {
        Source?.Dispose();
        GC.SuppressFinalize(this);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (Source is not null)
        {
            await Source.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    public static async Task<PMTilesReader?> FromUrl(string url)
    {
        var webSource = new WebSource(url);
        var available = await webSource.IsAvailable();
        return available ? new PMTilesReader(webSource) : null;
    }

    public static PMTilesReader? FromFile(string path)
    {
        return File.Exists(path) ? new PMTilesReader(new StreamSource(File.OpenRead(path))) : null;
    }

    public async ValueTask<Header> GetHeader()
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }

        var (header, _) = await Source.GetHeaderAndRoot();
        return header;
    }

    public async Task<Stream?> GetTileZxy(int z, int x, int y)
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }
        return await Source.GetTile(PMTilesHelper.ZxyToTileId(z, x, y));
    }
    
    public async Task<byte[]?> GetTileZxyAsBytes(int z, int x, int y)
    {
        var stream = await GetTileZxy(z, x, y);
        if (stream is null) {
            return null;
        }
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}