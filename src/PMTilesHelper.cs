using System.IO.Compression;
using System.Runtime.InteropServices;

namespace PMTiles;

/// <summary>
/// A helper class for PMTiles
/// </summary>
internal static class PMTilesHelper
{
    public const int HeaderSize = 127;
    
    public static MemoryStream CreateBinaryReader(Memory<byte> memory)
    {
        return MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment) ?
            // if the Memory<T> is backed by an array, we can use the array directly
            new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false) :
            // otherwise, copy the data to a new array
            new MemoryStream(memory.ToArray(), writable: false);
    }
    
    public static Stream Decompress(Stream stream, Compression compression)
    {
        return compression switch
        {
            Compression.Gzip => new GZipStream(stream, CompressionMode.Decompress),
            Compression.Brotli => new BrotliStream(stream, CompressionMode.Decompress),
            // Zstandard Compression is currently not supported
            _ => stream
        };
    }
    
    /// <summary>
    /// Read a varint from a stream.
    ///
    /// Varint means a variable-length integer.
    /// </summary>
    /// <param name="stream">source</param>
    /// <returns>uint64 value</returns>
    /// <exception cref="EndOfStreamException">When the end of the stream is reached</exception>
    /// <exception cref="InvalidOperationException">When the varint is too long to fit in a ulong</exception>
    public static ulong ReadVarint(this Stream stream)
    {
        ulong val = 0;
        var shift = 0;

        while (true)
        {
            var b = stream.ReadByte();

            if (b == -1)
                throw new EndOfStreamException("Unexpected end of stream");

            val |= ((ulong)b & 0x7F) << shift;

            if (b < 0x80)
                return val;

            shift += 7;

            if (shift >= 64)
                throw new InvalidOperationException("Varint is too long");
        }
    }
    private static readonly long[] TzValues =
    [
        0, 1, 5, 21, 85, 341, 1365, 5461, 21845, 87381, 
        349525, 1398101, 5592405, 22369621, 89478485, 
        357913941, 1431655765, 5726623061, 22906492245, 
        91625968981, 366503875925, 1466015503701, 
        5864062014805, 23456248059221, 93824992236885, 
        375299968947541, 1501199875790165
    ];

    private static void Rotate(long n, long[] xy, int rx, int ry)
    {
        if (ry != 0) return;
        if (rx == 1)
        {
            xy[0] = n - 1 - xy[0];
            xy[1] = n - 1 - xy[1];
        }
        (xy[0], xy[1]) = (xy[1], xy[0]);
    }

    public static long ZxyToTileId(int z, int x, int y)
    {
        if (z > 26)
        {
            throw new ArgumentOutOfRangeException(nameof(z), "Tile zoom level exceeds max safe number limit (26)");
        }
        if (x > (1 << z) - 1 || y > (1 << z) - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "tile x/y outside zoom level bounds");
        }

        var acc = TzValues[z];
        long n = 1 << z; // 2^z
        long d = 0;
        long[] xy = [x, y];
        var s = n / 2;

        while (s > 0)
        {
            var rx = (xy[0] & s) > 0 ? 1 : 0;
            var ry = (xy[1] & s) > 0 ? 1 : 0;
            d += s * s * ((3 * rx) ^ ry);
            Rotate(s, xy, rx, ry);
            s /= 2; // s = s / 2
        }
        return acc + d;
    }
}