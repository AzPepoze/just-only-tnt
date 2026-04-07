namespace justonlytnt.World.Jobs;

public interface IChunkJob
{
    int Priority { get; }
    IChunkJobResult Execute();
}

public interface IChunkJobResult
{
}
