using System.Collections.Generic;
using Godot;

namespace justonlytnt.World;

public sealed partial class VoxelWorld
{
    public BlockType GetBlock(Vector3I globalPos)
    {
        if (globalPos.Y < 0 || globalPos.Y >= Config.ChunkHeight)
        {
            return BlockType.Air;
        }

        ChunkCoord chunkCoord = WorldToChunk(globalPos);
        if (!_chunks.TryGetValue(chunkCoord, out ChunkRuntime? runtime) || runtime.Data is null)
        {
            return BlockType.Air;
        }

        Vector3I local = WorldToLocal(globalPos);
        return runtime.Data.Get(local.X, local.Y, local.Z);
    }

	public void SetBlock(Vector3I globalPos, BlockType type)
	{
		if (globalPos.Y < 0 || globalPos.Y >= Config.ChunkHeight)
		{
            return;
        }

        ChunkCoord coord = WorldToChunk(globalPos);
        if (!_chunks.TryGetValue(coord, out ChunkRuntime? runtime) || runtime.Data is null)
        {
            return;
        }

        Vector3I local = WorldToLocal(globalPos);
        runtime.Data.Set(local.X, local.Y, local.Z, type);
        runtime.Version++;
        runtime.CollisionReady = false;
        EnqueueMeshRebuild(coord, runtime, PriorityFor(coord));

        if (local.X == 0) TouchNeighbor(new ChunkCoord(coord.X - 1, coord.Z));
        if (local.X == Config.ChunkSize - 1) TouchNeighbor(new ChunkCoord(coord.X + 1, coord.Z));
		if (local.Z == 0) TouchNeighbor(new ChunkCoord(coord.X, coord.Z - 1));
		if (local.Z == Config.ChunkSize - 1) TouchNeighbor(new ChunkCoord(coord.X, coord.Z + 1));
	}

	public void QueueSetBlocksFromPacked(
		Godot.Collections.Array<int> packed,
		BlockType defaultType,
		int stride = 4,
		bool usePackedType = false)
	{
		if (packed.Count < 3)
		{
			return;
		}

		int safeStride = Mathf.Max(3, stride);
		HashSet<ChunkCoord> dirtyChunks = new();
		HashSet<ChunkCoord> remeshChunks = new();

		for (int i = 0; i + 2 < packed.Count; i += safeStride)
		{
			Vector3I globalPos = new(packed[i], packed[i + 1], packed[i + 2]);
			BlockType type = defaultType;
			if (usePackedType && safeStride > 3 && i + 3 < packed.Count)
			{
				type = (BlockType)packed[i + 3];
			}

			ApplyBatchedBlockSet(globalPos, type, dirtyChunks, remeshChunks);
		}

		ApplyBatchedBlockSetRemesh(dirtyChunks, remeshChunks);
	}

	public ExplosionResult ApplyExplosion(Vector3 center, float radius, float power)
	{
        int minX = Mathf.FloorToInt(center.X - radius);
        int maxX = Mathf.CeilToInt(center.X + radius);
        int minY = Mathf.FloorToInt(center.Y - radius);
        int maxY = Mathf.CeilToInt(center.Y + radius);
        int minZ = Mathf.FloorToInt(center.Z - radius);
        int maxZ = Mathf.CeilToInt(center.Z + radius);

        float radiusSq = radius * radius;

        HashSet<ChunkCoord> dirtyChunks = new();
        HashSet<ChunkCoord> remeshChunks = new();
        List<RemovedBlock> removedBlocks = new();

        for (int y = minY; y <= maxY; y++)
        {
            if (y < 0 || y >= Config.ChunkHeight)
            {
                continue;
            }

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector3 delta = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) - center;
                    if (delta.LengthSquared() > radiusSq)
                    {
                        continue;
                    }

                    Vector3I blockPos = new(x, y, z);
                    ChunkCoord coord = WorldToChunk(blockPos);
                    if (!_chunks.TryGetValue(coord, out ChunkRuntime? runtime) || runtime.Data is null)
                    {
                        continue;
                    }

                    Vector3I local = WorldToLocal(blockPos);
                    BlockType removedType = runtime.Data.Get(local.X, local.Y, local.Z);
                    if (removedType == BlockType.Air)
                    {
                        continue;
                    }

                    runtime.Data.Set(local.X, local.Y, local.Z, BlockType.Air);
                    runtime.Version++;
                    runtime.CollisionReady = false;
                    dirtyChunks.Add(coord);
                    remeshChunks.Add(coord);
                    removedBlocks.Add(new RemovedBlock(blockPos, removedType));

                    if (local.X == 0) remeshChunks.Add(new ChunkCoord(coord.X - 1, coord.Z));
                    if (local.X == Config.ChunkSize - 1) remeshChunks.Add(new ChunkCoord(coord.X + 1, coord.Z));
                    if (local.Z == 0) remeshChunks.Add(new ChunkCoord(coord.X, coord.Z - 1));
                    if (local.Z == Config.ChunkSize - 1) remeshChunks.Add(new ChunkCoord(coord.X, coord.Z + 1));
                }
            }
        }

        foreach (ChunkCoord coord in remeshChunks)
        {
            if (_chunks.TryGetValue(coord, out ChunkRuntime? runtime) && runtime.Data is not null)
            {
                if (!dirtyChunks.Contains(coord))
                {
                    runtime.Version++;
                    runtime.CollisionReady = false;
                }

                EnqueueMeshRebuild(coord, runtime, PriorityFor(coord));
            }
        }

        if (power > 0f)
        {
            // Reserved for additional impulse integration.
        }

		return new ExplosionResult(removedBlocks);
	}

	public bool IsChunkLoaded(ChunkCoord coord) => _chunks.TryGetValue(coord, out ChunkRuntime? runtime) && runtime.Data is not null;

	public bool IsChunkCollisionReady(ChunkCoord coord) => _chunks.TryGetValue(coord, out ChunkRuntime? runtime) && runtime.CollisionReady;

	private void ApplyBatchedBlockSet(
		Vector3I globalPos,
		BlockType type,
		HashSet<ChunkCoord> dirtyChunks,
		HashSet<ChunkCoord> remeshChunks)
	{
		if (globalPos.Y < 0 || globalPos.Y >= Config.ChunkHeight)
		{
			return;
		}

		ChunkCoord coord = WorldToChunk(globalPos);
		if (!_chunks.TryGetValue(coord, out ChunkRuntime? runtime) || runtime.Data is null)
		{
			return;
		}

		Vector3I local = WorldToLocal(globalPos);
		if (runtime.Data.Get(local.X, local.Y, local.Z) == type)
		{
			return;
		}

		runtime.Data.Set(local.X, local.Y, local.Z, type);
		dirtyChunks.Add(coord);
		remeshChunks.Add(coord);

		if (local.X == 0) remeshChunks.Add(new ChunkCoord(coord.X - 1, coord.Z));
		if (local.X == Config.ChunkSize - 1) remeshChunks.Add(new ChunkCoord(coord.X + 1, coord.Z));
		if (local.Z == 0) remeshChunks.Add(new ChunkCoord(coord.X, coord.Z - 1));
		if (local.Z == Config.ChunkSize - 1) remeshChunks.Add(new ChunkCoord(coord.X, coord.Z + 1));
	}

	private void ApplyBatchedBlockSetRemesh(HashSet<ChunkCoord> dirtyChunks, HashSet<ChunkCoord> remeshChunks)
	{
		if (remeshChunks.Count == 0)
		{
			return;
		}

		foreach (ChunkCoord coord in dirtyChunks)
		{
			if (_chunks.TryGetValue(coord, out ChunkRuntime? runtime) && runtime.Data is not null)
			{
				runtime.Version++;
				runtime.CollisionReady = false;
			}
		}

		foreach (ChunkCoord coord in remeshChunks)
		{
			if (!_chunks.TryGetValue(coord, out ChunkRuntime? runtime) || runtime.Data is null)
			{
				continue;
			}

			if (!dirtyChunks.Contains(coord))
			{
				runtime.Version++;
				runtime.CollisionReady = false;
			}

			EnqueueMeshRebuild(coord, runtime, PriorityFor(coord));
		}
	}
}
