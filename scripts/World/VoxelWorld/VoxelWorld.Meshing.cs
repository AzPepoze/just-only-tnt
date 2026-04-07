using justonlytnt.World.Jobs;

namespace justonlytnt.World;

public sealed partial class VoxelWorld
{
    private void DrainResults()
    {
        while (_resultQueue.TryDequeue(out IChunkJobResult? result))
        {
            switch (result)
            {
                case GenerateChunkResult gen:
                    HandleGenerateResult(gen);
                    break;
                case BuildChunkMeshResult mesh:
                    _pendingMeshApply.Enqueue(mesh);
                    break;
            }
        }
    }

    private void HandleGenerateResult(GenerateChunkResult result)
    {
        if (!_chunks.TryGetValue(result.Coord, out ChunkRuntime? runtime))
        {
            return;
        }

        runtime.Data = result.Data;
        runtime.GenerationRequested = false;
        runtime.Version++;
        runtime.CollisionReady = false;
        EnqueueMeshRebuild(result.Coord, runtime, PriorityFor(result.Coord));
        RemeshExistingNeighbors(result.Coord);
    }

    private void ApplyMeshResults()
    {
        int applyBudget = Config.MaxMainThreadAppliesPerFrame;
        int collisionBudget = Config.MaxCollisionBuildsPerFrame;

        for (int i = 0; i < applyBudget && _pendingMeshApply.Count > 0; i++)
        {
            BuildChunkMeshResult meshResult = _pendingMeshApply.Dequeue();
            if (!_chunks.TryGetValue(meshResult.Coord, out ChunkRuntime? runtime))
            {
                continue;
            }

            if (runtime.Version != meshResult.Version)
            {
                runtime.MeshRequested = false;
                if (runtime.Data is not null)
                {
                    EnqueueMeshRebuild(meshResult.Coord, runtime, PriorityFor(meshResult.Coord));
                }
                continue;
            }

            bool updateCollision = collisionBudget > 0;
            runtime.Renderer.ApplyMesh(meshResult.Mesh, updateCollision);
            runtime.MeshRequested = false;

            if (updateCollision)
            {
                runtime.CollisionReady = true;
                collisionBudget--;
            }
        }
    }

    private void EnqueueMeshRebuild(ChunkCoord coord, ChunkRuntime runtime, int priority)
    {
        if (runtime.Data is null || runtime.MeshRequested)
        {
            return;
        }

        runtime.MeshRequested = true;
        ChunkData snapshot = runtime.Data.Clone();
        ChunkMesher.NeighborChunks neighbors = CaptureNeighborSnapshots(coord);
        int version = runtime.Version;
        ScheduleJob(new BuildChunkMeshJob(coord, snapshot, neighbors, version, priority));
    }

    private void TouchNeighbor(ChunkCoord coord)
    {
        if (_chunks.TryGetValue(coord, out ChunkRuntime? runtime) && runtime.Data is not null)
        {
            runtime.Version++;
            runtime.CollisionReady = false;
            EnqueueMeshRebuild(coord, runtime, PriorityFor(coord));
        }
    }

    private void RemeshExistingNeighbors(ChunkCoord coord)
    {
        TouchNeighbor(new ChunkCoord(coord.X + 1, coord.Z));
        TouchNeighbor(new ChunkCoord(coord.X - 1, coord.Z));
        TouchNeighbor(new ChunkCoord(coord.X, coord.Z + 1));
        TouchNeighbor(new ChunkCoord(coord.X, coord.Z - 1));
    }

    private ChunkMesher.NeighborChunks CaptureNeighborSnapshots(ChunkCoord coord)
    {
        ChunkData? posX = TryCloneChunkData(new ChunkCoord(coord.X + 1, coord.Z));
        ChunkData? negX = TryCloneChunkData(new ChunkCoord(coord.X - 1, coord.Z));
        ChunkData? posZ = TryCloneChunkData(new ChunkCoord(coord.X, coord.Z + 1));
        ChunkData? negZ = TryCloneChunkData(new ChunkCoord(coord.X, coord.Z - 1));

        return new ChunkMesher.NeighborChunks(posX, negX, posZ, negZ, treatMissingAsSolid: true);
    }

    private ChunkData? TryCloneChunkData(ChunkCoord coord)
    {
        if (_chunks.TryGetValue(coord, out ChunkRuntime? runtime) && runtime.Data is not null)
        {
            return runtime.Data.Clone();
        }

        return null;
    }

    private int PriorityFor(ChunkCoord coord)
    {
        if (PlayerTarget is null)
        {
            return 0;
        }

        ChunkCoord playerChunk = WorldToChunk(new Godot.Vector3I(
            Godot.Mathf.FloorToInt(PlayerTarget.GlobalPosition.X),
            Godot.Mathf.FloorToInt(PlayerTarget.GlobalPosition.Y),
            Godot.Mathf.FloorToInt(PlayerTarget.GlobalPosition.Z)));

        int dx = coord.X - playerChunk.X;
        int dz = coord.Z - playerChunk.Z;
        return (dx * dx) + (dz * dz);
    }
}
