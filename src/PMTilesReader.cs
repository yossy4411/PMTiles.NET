using PMTiles.Sources;

namespace PMTiles;

public class PMTilesReader(Source source)
{
    private Source? Source { get; set; } = source;

    public static async Task<PMTilesReader?> FromUrl(string url)
    {
        var client = new HttpClient();
        try
        {
            var st = await client.GetStreamAsync(url);
            return new PMTilesReader(new CachedSource(st));
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            client.Dispose();
        }
    }

    public static PMTilesReader? FromFile(string path)
    {
        return File.Exists(path) ? new PMTilesReader(new CachedSource(File.OpenRead(path))) : null;
    }

    public async Task<Header> GetHeader()
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }

        var (header, _) = await Source.GetHeaderAndRoot();
        return header;
    }

    public async Task<byte[]?> GetTileZxy(int z, int x, int y)
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }
        return await Source.GetTile(PMTilesHelper.ZxyToTileId(z, x, y));
    }
}