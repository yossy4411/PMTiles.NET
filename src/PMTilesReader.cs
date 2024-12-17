using PMTiles.Sources;

namespace PMTiles;

/// <summary>
/// A reader for PMTiles
/// </summary>
public class PMTilesReader : IDisposable, IAsyncDisposable
{
    private Source? Source { get; set; }
    
    /// <summary>
    /// A reader for PMTiles
    /// </summary>
    /// <param name="source">Source</param>
    public PMTilesReader(Source source)
    {
        Source = source;
    }
    
    /// <summary>
    /// Dispose the reader
    /// </summary>
    /// <returns></returns>
    public void Dispose()
    {
        Source?.Dispose();
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Dispose the reader
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        if (Source is not null)
        {
            await Source.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Create a reader from a URL
    /// </summary>
    /// <param name="url">URL</param>
    /// <returns>Reader</returns>
    public static async Task<PMTilesReader?> FromUrlAsync(string url)
    {
        var webSource = new WebSource(url);
        var available = await webSource.IsAvailableAsync();
        return available ? new PMTilesReader(webSource) : null;
    }
    
    /// <summary>
    /// Create a reader from a URL
    /// </summary>
    /// <param name="url">URL</param>
    /// <returns>Reader</returns>
    public static PMTilesReader? FromUrl(string url)
    {
        var webSource = new WebSource(url);
        var available = webSource.IsAvailable();
        return available ? new PMTilesReader(webSource) : null;
    }
    
    /// <summary>
    /// Create a reader from a file
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>Reader</returns>
    public static PMTilesReader? FromFile(string path)
    {
        return File.Exists(path) ? new PMTilesReader(new StreamSource(File.OpenRead(path))) : null;
    }
    
    /// <summary>
    /// Get the header
    /// </summary>
    /// <returns>Header</returns>
    /// <exception cref="InvalidOperationException">Returns if the source has not been set</exception>
    public async ValueTask<Header> GetHeaderAsync()
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }

        var (header, _) = await Source.GetHeaderAndRoot();
        return header;
    }
    
    /// <summary>
    /// Get the header
    /// </summary>
    /// <returns>Header</returns>
    /// <exception cref="InvalidOperationException">Returns if the source has not been set</exception>
    public Header GetHeader()
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }

        return Source.GetHeaderSync();
    }

    /// <summary>
    /// Get the tile as a stream
    /// </summary>
    /// <param name="z">zoom</param>
    /// <param name="x">x</param>
    /// <param name="y">y</param>
    /// <returns>Stream that contains the tile</returns>
    /// <exception cref="InvalidOperationException">Returns if the source has not been set</exception>
    public async Task<Stream?> GetTileZxyAsync(int z, int x, int y)
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }
        return await Source.GetTile(PMTilesHelper.ZxyToTileId(z, x, y));
    }
    
    /// <summary>
    /// Get the tile as a stream
    /// </summary>
    /// <param name="z">zoom</param>
    /// <param name="x">x</param>
    /// <param name="y">y</param>
    /// <returns>Stream that contains the tile</returns>
    /// <exception cref="InvalidOperationException">Returns if the source has not been set</exception>
    public async Task<byte[]?> GetTileZxyAsBytesAsync(int z, int x, int y)
    {
        var stream = await GetTileZxyAsync(z, x, y);
        if (stream is null) {
            return null;
        }
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}