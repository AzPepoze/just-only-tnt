using Godot;

namespace justonlytnt.World.Jobs;

public sealed class GenerateChunkJob : IChunkJob
{
    private readonly ChunkCoord _coord;
    private readonly WorldConfig _config;

    public int Priority { get; }

    public GenerateChunkJob(ChunkCoord coord, WorldConfig config, int priority)
    {
        _coord = coord;
        _config = config;
        Priority = priority;
    }

    public IChunkJobResult Execute()
    {
        ChunkData data = new(_config.ChunkSize, _config.ChunkHeight);

        for (int z = 0; z < _config.ChunkSize; z++)
        {
            for (int x = 0; x < _config.ChunkSize; x++)
            {
                int globalX = (_coord.X * _config.ChunkSize) + x;
                int globalZ = (_coord.Z * _config.ChunkSize) + z;

                float heightNoise = Fbm(globalX * _config.TerrainFrequency, globalZ * _config.TerrainFrequency, _config.Seed);
                int height = Mathf.Clamp(
                    _config.BaseTerrainHeight + Mathf.RoundToInt(heightNoise * _config.TerrainAmplitude),
                    1,
                    _config.ChunkHeight - 1);

                for (int y = 0; y <= height; y++)
                {
                    BlockType type = y == height ? BlockType.Grass : BlockType.Dirt;
                    data.Set(x, y, z, type);
                }
            }
        }

        return new GenerateChunkResult(_coord, data);
    }

    private static float Fbm(float x, float z, int seed)
    {
        float value = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;

        for (int i = 0; i < 4; i++)
        {
            value += ValueNoise(x * frequency, z * frequency, seed + (i * 101)) * amplitude;
            frequency *= 2f;
            amplitude *= 0.5f;
        }

        return value;
    }

    private static float ValueNoise(float x, float z, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int z0 = Mathf.FloorToInt(z);
        int x1 = x0 + 1;
        int z1 = z0 + 1;

        float tx = x - x0;
        float tz = z - z0;

        float sx = SmoothStep(tx);
        float sz = SmoothStep(tz);

        float n00 = HashToUnit(x0, z0, seed);
        float n10 = HashToUnit(x1, z0, seed);
        float n01 = HashToUnit(x0, z1, seed);
        float n11 = HashToUnit(x1, z1, seed);

        float ix0 = Mathf.Lerp(n00, n10, sx);
        float ix1 = Mathf.Lerp(n01, n11, sx);

        return Mathf.Lerp(ix0, ix1, sz);
    }

    private static float SmoothStep(float t) => t * t * (3f - (2f * t));

    private static float HashToUnit(int x, int z, int seed)
    {
        uint h = (uint)(x * 374761393) ^ (uint)(z * 668265263) ^ (uint)(seed * 1013904223);
        h ^= h >> 13;
        h *= 1274126177;
        h ^= h >> 16;
        return ((h & 0x00FFFFFFu) / 16777215f) * 2f - 1f;
    }
}

public sealed class GenerateChunkResult : IChunkJobResult
{
    public readonly ChunkCoord Coord;
    public readonly ChunkData Data;

    public GenerateChunkResult(ChunkCoord coord, ChunkData data)
    {
        Coord = coord;
        Data = data;
    }
}
