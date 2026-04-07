using System;

namespace justonlytnt.World;

public sealed class ChunkData
{
    private readonly byte[] _blocks;

    public int Size { get; }
    public int Height { get; }

    public ChunkData(int size, int height)
    {
        Size = size;
        Height = height;
        _blocks = new byte[size * height * size];
    }

    private ChunkData(int size, int height, byte[] blocks)
    {
        Size = size;
        Height = height;
        _blocks = blocks;
    }

    public BlockType Get(int x, int y, int z)
    {
        if ((uint)x >= (uint)Size || (uint)y >= (uint)Height || (uint)z >= (uint)Size)
        {
            return BlockType.Air;
        }

        return (BlockType)_blocks[Index(x, y, z)];
    }

    public void Set(int x, int y, int z, BlockType type)
    {
        if ((uint)x >= (uint)Size || (uint)y >= (uint)Height || (uint)z >= (uint)Size)
        {
            return;
        }

        _blocks[Index(x, y, z)] = (byte)type;
    }

    public bool IsSolid(int x, int y, int z) => Get(x, y, z) != BlockType.Air;

    public ChunkData Clone()
    {
        var copy = new byte[_blocks.Length];
        Array.Copy(_blocks, copy, _blocks.Length);
        return new ChunkData(Size, Height, copy);
    }

    public byte[] Raw => _blocks;

    private int Index(int x, int y, int z) => x + (z * Size) + (y * Size * Size);
}
