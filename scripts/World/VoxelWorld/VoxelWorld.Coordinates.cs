using Godot;

namespace justonlytnt.World;

public sealed partial class VoxelWorld
{
    public ChunkCoord WorldToChunk(Vector3I world)
    {
        int cx = FloorDiv(world.X, Config.ChunkSize);
        int cz = FloorDiv(world.Z, Config.ChunkSize);
        return new ChunkCoord(cx, cz);
    }

    public Vector3I WorldToLocal(Vector3I world)
    {
        int lx = PositiveMod(world.X, Config.ChunkSize);
        int lz = PositiveMod(world.Z, Config.ChunkSize);
        return new Vector3I(lx, world.Y, lz);
    }

    private static int FloorDiv(int value, int divisor)
    {
        int q = value / divisor;
        int r = value % divisor;
        if (r != 0 && ((r < 0) != (divisor < 0)))
        {
            q--;
        }

        return q;
    }

    private static int PositiveMod(int value, int mod)
    {
        int r = value % mod;
        return r < 0 ? r + mod : r;
    }
}
