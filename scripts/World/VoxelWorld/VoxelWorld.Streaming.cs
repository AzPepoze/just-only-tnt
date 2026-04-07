using System.Collections.Generic;
using Godot;
using justonlytnt.World.Jobs;

namespace justonlytnt.World;

public sealed partial class VoxelWorld
{
    private void UpdateVisibleChunks(Vector3 playerWorldPosition)
    {
        ChunkCoord center = WorldToChunk(new Vector3I(
            Mathf.FloorToInt(playerWorldPosition.X),
            Mathf.FloorToInt(playerWorldPosition.Y),
            Mathf.FloorToInt(playerWorldPosition.Z)));

        _visibleSet.Clear();
        int radius = Config.ViewDistanceChunks;

        for (int dz = -radius; dz <= radius; dz++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                ChunkCoord coord = new(center.X + dx, center.Z + dz);
                int distSq = (dx * dx) + (dz * dz);
                if (distSq > radius * radius)
                {
                    continue;
                }

                _visibleSet.Add(coord);

                if (!_chunks.TryGetValue(coord, out ChunkRuntime? runtime))
                {
                    if (_pendingRemoveSet.Remove(coord))
                    {
                        RemoveFromQueue(_pendingRemoveQueue, coord);
                    }

                    if (_pendingCreateSet.Add(coord))
                    {
                        _pendingCreateQueue.Enqueue(coord);
                    }
                    continue;
                }

                if (!runtime.GenerationRequested && runtime.Data is null)
                {
                    runtime.GenerationRequested = true;
                    ScheduleJob(new GenerateChunkJob(coord, Config, distSq));
                }
            }
        }

        foreach ((ChunkCoord coord, ChunkRuntime _) in _chunks)
        {
            if (!_visibleSet.Contains(coord))
            {
                if (_pendingRemoveSet.Add(coord))
                {
                    _pendingRemoveQueue.Enqueue(coord);
                }
            }
            else if (_pendingRemoveSet.Remove(coord))
            {
                RemoveFromQueue(_pendingRemoveQueue, coord);
            }
        }
    }

    private ChunkRuntime CreateChunkRuntime(ChunkCoord coord)
    {
        ChunkRenderer renderer = _rendererPool.Count > 0 ? _rendererPool.Dequeue() : new ChunkRenderer();
        if (renderer.GetParent() != this)
        {
            AddChild(renderer);
        }

        renderer.Name = $"Chunk_{coord.X}_{coord.Z}";
        renderer.Position = new Vector3(coord.X * Config.ChunkSize, 0, coord.Z * Config.ChunkSize);
        renderer.Visible = true;

        return new ChunkRuntime(renderer);
    }

    private void ProcessChunkStreaming()
    {
        int createBudget = Config.MaxChunkCreatesPerFrame;
        for (int i = 0; i < createBudget && _pendingCreateQueue.Count > 0; i++)
        {
            ChunkCoord coord = _pendingCreateQueue.Dequeue();
            _pendingCreateSet.Remove(coord);

            if (!_visibleSet.Contains(coord) || _chunks.ContainsKey(coord))
            {
                continue;
            }

            ChunkRuntime runtime = CreateChunkRuntime(coord);
            _chunks.Add(coord, runtime);
            runtime.GenerationRequested = true;
            ScheduleJob(new GenerateChunkJob(coord, Config, PriorityFor(coord)));
        }

        int removeBudget = Config.MaxChunkRemovalsPerFrame;
        for (int i = 0; i < removeBudget && _pendingRemoveQueue.Count > 0; i++)
        {
            ChunkCoord coord = _pendingRemoveQueue.Dequeue();
            _pendingRemoveSet.Remove(coord);

            if (_visibleSet.Contains(coord))
            {
                continue;
            }

            if (_chunks.TryGetValue(coord, out ChunkRuntime? runtime))
            {
                ReleaseChunkRuntime(coord, runtime);
            }
        }
    }

    private void ReleaseChunkRuntime(ChunkCoord coord, ChunkRuntime runtime)
    {
        runtime.Renderer.ResetForPool();
        _rendererPool.Enqueue(runtime.Renderer);
        _chunks.Remove(coord);
    }

    private static void RemoveFromQueue(Queue<ChunkCoord> queue, ChunkCoord target)
    {
        int count = queue.Count;
        for (int i = 0; i < count; i++)
        {
            ChunkCoord current = queue.Dequeue();
            if (!current.Equals(target))
            {
                queue.Enqueue(current);
            }
        }
    }

    private void UpdateChunkVisibility(Vector3 playerWorldPosition)
    {
        float maxDistance = Config.ViewDistanceChunks * Config.ChunkSize * 1.2f;
        float maxDistanceSq = maxDistance * maxDistance;

        foreach ((ChunkCoord _, ChunkRuntime runtime) in _chunks)
        {
            Vector3 toChunk = runtime.Renderer.GlobalPosition - playerWorldPosition;
            runtime.Renderer.Visible = toChunk.LengthSquared() <= maxDistanceSq;
        }
    }
}
