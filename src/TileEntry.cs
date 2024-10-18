using System.Diagnostics.CodeAnalysis;

namespace PMTiles;

public class TileEntry
{
    public ulong TileId { get; init; }
    public ulong Offset { get; internal set; }
    public ulong Length { get; internal set; }
    public ulong RunLength { get; internal set; }
}

public readonly struct MemoryPosition : IEquatable<MemoryPosition>
{
    public MemoryPosition(ulong offset, ulong length)
    {
        Offset = offset;
        Length = length;
    }

    public ulong Offset { get; init; }
    public ulong Length { get; init; }
    
    public void Deconstruct(out ulong offset, out ulong length) => (offset, length) = (Offset, Length);

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset, Length);
    }

    public static bool operator ==(MemoryPosition left, MemoryPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MemoryPosition left, MemoryPosition right)
    {
        return !(left == right);
    }

    public bool Equals(MemoryPosition other)
    {
        return Offset == other.Offset && Length == other.Length;
    }

    public override bool Equals(object? obj)
    {
        return obj is MemoryPosition other && Equals(other);
    }
}