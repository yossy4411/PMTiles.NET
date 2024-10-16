# PMTiles.NET

A simple library for reading PMTiles files.

# ⚠Caution⚠

This library is a port of the [PMTiles](https://github.com/protomaps/PMTiles) (JavaScript/C++) library for .NET. It is not yet fully tested and may contain bugs. Use at your own risk.

# Usage

```csharp
using PMTiles;

var pmTiles = PMTilesReader.FromFile("path/to/file.pmtiles");

var metadata = pmTiles.Metadata;
Console.WriteLine(metadata.Name);

var tile = pmTiles.GetTile(0, 0, 0);

```

This example reads the metadata and the tile at zoom level 0, x 0, y 0.