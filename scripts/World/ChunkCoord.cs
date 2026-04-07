using System;
using Godot;

namespace justonlytnt.World;

public readonly struct ChunkCoord : IEquatable<ChunkCoord>
{
    public readonly int X;
    public readonly int Z;

    public ChunkCoord(int x, int z)
    {
        X = x;
        Z = z;
    }

    public bool Equals(ChunkCoord other) => X == other.X && Z == other.Z;

    public override bool Equals(object? obj) => obj is ChunkCoord other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Z);

    public Vector2I ToVector2I() => new(X, Z);

    public override string ToString() => $"({X}, {Z})";
}
