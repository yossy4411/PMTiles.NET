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
    
}