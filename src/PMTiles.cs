namespace PMTiles.NET;

public class PMTiles
{
    private Source? Source { get; set; }
    
    public async Task<bool> CreateFromUrl(string url)
    {
        var st = await new HttpClient().GetStreamAsync(url);
        Source = new Source(st);
        return Source != null;
    }
    
    public async Task<Header> GetHeader()
    {
        if (Source == null)
        {
            throw new InvalidOperationException("Source is not set");
        }

        return await Source.GetHeader();
    }
}