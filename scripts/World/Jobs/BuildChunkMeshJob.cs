namespace justonlytnt.World.Jobs;

public sealed class BuildChunkMeshJob : IChunkJob
{
    private readonly ChunkCoord _coord;
    private readonly ChunkData _snapshot;
    private readonly ChunkMesher.NeighborChunks _neighbors;
    private readonly int _version;

    public int Priority { get; }

    public BuildChunkMeshJob(ChunkCoord coord, ChunkData snapshot, ChunkMesher.NeighborChunks neighbors, int version, int priority)
    {
        _coord = coord;
        _snapshot = snapshot;
        _neighbors = neighbors;
        _version = version;
        Priority = priority;
    }

    // Backward-compatible overload for call sites still using the old signature.
    public BuildChunkMeshJob(ChunkCoord coord, ChunkData snapshot, int version, int priority)
        : this(coord, snapshot, new ChunkMesher.NeighborChunks(null, null, null, null, true), version, priority)
    {
    }

    public IChunkJobResult Execute()
    {
        MeshBuildData mesh = ChunkMesher.Build(_snapshot, _neighbors);
        return new BuildChunkMeshResult(_coord, _version, mesh);
    }
}

public sealed class BuildChunkMeshResult : IChunkJobResult
{
    public readonly ChunkCoord Coord;
    public readonly int Version;
    public readonly MeshBuildData Mesh;

    public BuildChunkMeshResult(ChunkCoord coord, int version, MeshBuildData mesh)
    {
        Coord = coord;
        Version = version;
        Mesh = mesh;
    }
}
