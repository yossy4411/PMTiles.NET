# PMTiles.NET

A simple library for reading PMTiles files.

## ⚠Caution⚠

This library is a port of the [PMTiles](https://github.com/protomaps/PMTiles) (Node.js) library for .NET. It is not yet fully tested and may contain bugs. Use at your own risk.

## Usage

```csharp
using PMTiles;

var pmTiles = PMTilesReader.FromFile("path/to/file.pmtiles");
// or PMTilesReader.FromUrl("https://example.com/file.pmtiles");

var header = pmTiles.GetHeader();
Console.WriteLine(header.Name);

Stream tile = pmTiles.GetTileZxy(0, 0, 0);

// Do something with the tile
```

This example reads the metadata and the tile at zoom level 0, x 0, y 0.

## Installation

This library is available on NuGet.

```bash
dotnet add package PMTiles.NET
```

## Notes

The writer of this library, `yossy4411`, is Japanese. If you find any mistakes with my English, please let me know.

Please help me develop this library by reporting issues and contributing to the code.   

## Special Thanks

- [protomaps](https://github.com/protomaps/) for creating the original PMTiles library.

## License

[MIT](LICENSE)
