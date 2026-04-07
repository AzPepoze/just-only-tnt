using System.Collections.Generic;
using Godot;

namespace justonlytnt.World;

public readonly struct RemovedBlock
{
    public readonly Vector3I Position;
    public readonly BlockType Type;

    public RemovedBlock(Vector3I position, BlockType type)
    {
        Position = position;
        Type = type;
    }
}

public sealed class ExplosionResult
{
    public readonly List<RemovedBlock> RemovedBlocks;

    public ExplosionResult(List<RemovedBlock> removedBlocks)
    {
        RemovedBlocks = removedBlocks;
    }
}
