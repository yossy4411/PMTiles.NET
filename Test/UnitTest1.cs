using Mapbox.Vector.Tile;
using PMTiles;

namespace Test;

/// <summary>
/// Tests for reading PMTiles
/// </summary>
public class Tests
{
    private PMTilesReader? _reader;
    
    [SetUp]
    public async Task Setup()
    {
        _reader = await PMTilesReader.FromUrl("https://cyberjapandata.gsi.go.jp/xyz/optimal_bvmap-v1/optimal_bvmap-v1.pmtiles");  // GSI Vector Tiles (地理院地図)
    }

    [Test]
    public async Task HeaderTest()
    {
        Assert.That(_reader, Is.Not.Null);
        var header = await _reader.GetHeader();
        Assert.Multiple(() =>
        {
            Assert.That(header.SpecVersion, Is.EqualTo(3)); // PMTiles Spec Version 3
            Assert.That(header.TileType, Is.EqualTo(TileType.Mvt)); // Mapbox Vector Tiles
        });
    }
    
    /// <summary>
    /// Test for reading a tile, at Tokyo, zoom level 9
    /// </summary>
    [Test]
    public async Task TileTest1()
    {
        Assert.That(_reader, Is.Not.Null);
        var tileId = PMTilesHelper.ZxyToTileId(9, 454, 201);  // at Tokyo, Tile ID is 286674
        Assert.That(tileId, Is.EqualTo(286674));
        
        var tile = await _reader.GetTileZxy(9, 454, 201);  // Tokyo
        Assert.That(tile, Is.Not.Null);

        var layers = VectorTileParser.Parse(tile);  // Mapbox.Vector.Tile
        Assert.That(layers, Is.Not.Null);
        Assert.That(layers, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(layers.Any(layer => layer.Name == "AdmBdry")); // layer name "AdmBdry" exists (Administrative Boundary 行政区域の境界)
            Assert.That(layers.Any(layer => layer.Name == "RailCL")); // layer name "RailCL" exists (Railway Center Line 鉄道中心線)
            Assert.That(layers.Any(layer => layer.Name == "RdCL")); // layer name "RdCL" exists (Road Center Line 道路中心線)
            Assert.That(layers.Any(layer => layer.Name == "WA")); // layer name "WA" exists (Water Area 水域)
        });
    }

    /// <summary>
    /// Test for reading a tile, at zoom level 14 (max)
    /// </summary>
    [Test]
    public async ValueTask TileTest2()
    {
        Assert.That(_reader, Is.Not.Null);
        var tileId = PMTilesHelper.ZxyToTileId(14, 14552, 6451);  // at Tokyo, Tile ID is 293539700
        Assert.That(tileId, Is.EqualTo(293554628));
        
        var tile = await _reader.GetTileZxy(14, 14552, 6451);  // Tokyo
        Assert.That(tile, Is.Not.Null);
        
        var layers = VectorTileParser.Parse(tile);  // Mapbox.Vector.Tile
        Assert.That(layers, Is.Not.Null);
        Assert.That(layers, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(layers.Any(layer => layer.Name == "AdmBdry"));  // layer name "AdmBdry" exists (Administrative Boundary 行政区域の境界)
            Assert.That(layers.Any(layer => layer.Name == "AdmArea"));  // layer name "AdmArea" exists (Administrative Area 行政区域)
            Assert.That(layers.Any(layer => layer.Name == "RailCL"));   // layer name "RailCL" exists (Railway Center Line 鉄道中心線)
            Assert.That(layers.Any(layer => layer.Name == "RdCL"));     // layer name "RdCL" exists (Road Center Line 道路中心線)
            Assert.That(layers.Any(layer => layer.Name == "WA"));       // layer name "WA" exists (Water Area 水域)
            Assert.That(layers.Any(layer => layer.Name == "BldA"));     // layer name "BldA" exists (Building area 建物の塗り)
            Assert.That(layers.Any(layer => layer.Name == "Anno"));     // layer name "Anno" exists (Annotation 注記)
        });
        // todo: Add more tests
    }
    
    // todo: Add more tests and implements e.g. Writing PMTiles, etc.
    
    [TearDown]
    public async ValueTask TearDown()
    {
        if (_reader is not null)
        {
            await _reader.DisposeAsync();
        }
    }
}