

var pm = new PMTiles.NET.PMTiles();

if (await pm.CreateFromUrl("https://cyberjapandata.gsi.go.jp/xyz/optimal_bvmap-v1/optimal_bvmap-v1.pmtiles"))
{

    var header = await pm.GetHeader();

    Console.WriteLine(header.SpecVersion);
}
else
{
    Console.WriteLine("Failed to create PMTiles");
}