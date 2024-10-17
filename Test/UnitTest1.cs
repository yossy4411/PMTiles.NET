using PMTiles;

namespace Test;

public class Tests
{
    private PMTilesReader? _reader;
    
    [SetUp]
    public async Task Setup()
    {
        _reader = await PMTilesReader.FromUrl("https://cyberjapandata.gsi.go.jp/xyz/optimal_bvmap-v1/optimal_bvmap-v1.pmtiles");  // GSI Vector Tiles (地理院地図)
    }

    [Test]
    public async Task Test1()
    {
        Assert.That(_reader, Is.Not.Null);
        var header = await _reader.GetHeader();
        Assert.Multiple(() =>
        {
            Assert.That(header.SpecVersion, Is.EqualTo(3)); // PMTiles Spec Version 3
            Assert.That(header.TileType, Is.EqualTo(TileType.Mvt)); // Mapbox Vector Tiles
        });

        var tile = await _reader.GetTileZxy(9, 454, 201);  // Tokyo
        Assert.That(tile, Is.Not.Null);
        Assert.That(tile!, Is.Not.Empty);
    }
}