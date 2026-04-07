using Godot;

namespace justonlytnt.World;

public sealed partial class VoxelWorld
{
    public long QueueOverwriteFill(Vector3I startInclusive, Vector3I endInclusive, BlockType type)
    {
        Vector3I min = new(
            Mathf.Min(startInclusive.X, endInclusive.X),
            Mathf.Min(startInclusive.Y, endInclusive.Y),
            Mathf.Min(startInclusive.Z, endInclusive.Z));

        Vector3I max = new(
            Mathf.Max(startInclusive.X, endInclusive.X),
            Mathf.Max(startInclusive.Y, endInclusive.Y),
            Mathf.Max(startInclusive.Z, endInclusive.Z));

        long volumeX = (long)max.X - min.X + 1L;
        long volumeY = (long)max.Y - min.Y + 1L;
        long volumeZ = (long)max.Z - min.Z + 1L;
        long volume = volumeX * volumeY * volumeZ;
        if (volume <= 0)
        {
            return 0;
        }

        _pendingFillOperations.Enqueue(new PendingFillOperation(min, max, type));
        _pendingFillBlocks += volume;
        return volume;
    }

    private void ProcessQueuedFillOperations()
    {
        if (_pendingFillOperations.Count == 0)
        {
            return;
        }

        int budget = Mathf.Max(1, Config.FillBlocksPerFrame);
        _fillDirtyChunks.Clear();
        _fillRemeshChunks.Clear();

        while (budget > 0 && _pendingFillOperations.Count > 0)
        {
            PendingFillOperation operation = _pendingFillOperations.Dequeue();

            while (budget > 0 && !operation.IsComplete)
            {
                ApplyFillBlockOverwrite(operation.Current, operation.Type);
                operation.Advance();
                budget--;
                _pendingFillBlocks = System.Math.Max(0L, _pendingFillBlocks - 1L);
            }

            if (!operation.IsComplete)
            {
                _pendingFillOperations.Enqueue(operation);
            }
        }

        ApplyBatchedRemeshChanges();
    }

    private void ApplyFillBlockOverwrite(Vector3I globalPos, BlockType type)
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
        _fillDirtyChunks.Add(coord);
        _fillRemeshChunks.Add(coord);

        if (local.X == 0) _fillRemeshChunks.Add(new ChunkCoord(coord.X - 1, coord.Z));
        if (local.X == Config.ChunkSize - 1) _fillRemeshChunks.Add(new ChunkCoord(coord.X + 1, coord.Z));
        if (local.Z == 0) _fillRemeshChunks.Add(new ChunkCoord(coord.X, coord.Z - 1));
        if (local.Z == Config.ChunkSize - 1) _fillRemeshChunks.Add(new ChunkCoord(coord.X, coord.Z + 1));
    }

    private void ApplyBatchedRemeshChanges()
    {
        if (_fillRemeshChunks.Count == 0)
        {
            return;
        }

        foreach (ChunkCoord coord in _fillDirtyChunks)
        {
            if (_chunks.TryGetValue(coord, out ChunkRuntime? runtime) && runtime.Data is not null)
            {
                runtime.Version++;
                runtime.CollisionReady = false;
            }
        }

        foreach (ChunkCoord coord in _fillRemeshChunks)
        {
            if (!_chunks.TryGetValue(coord, out ChunkRuntime? runtime) || runtime.Data is null)
            {
                continue;
            }

            if (!_fillDirtyChunks.Contains(coord))
            {
                runtime.Version++;
                runtime.CollisionReady = false;
            }

            EnqueueMeshRebuild(coord, runtime, PriorityFor(coord));
        }
    }
}
