using System.Collections.Generic;
using Godot;

namespace justonlytnt.World;

public static class ChunkMesher
{
    private static readonly Vector3I[] NeighborOffsets =
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1),
    };

    private static readonly Vector3[] FaceNormals =
    {
        Vector3.Right,
        Vector3.Left,
        Vector3.Up,
        Vector3.Down,
        Vector3.Back,
        Vector3.Forward,
    };

    private static readonly Vector3[][] FaceVertices =
    {
        new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
        new[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) },
        new[] { new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) },
        new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) },
        new[] { new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1) },
        new[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) },
    };

    public readonly struct NeighborChunks
    {
        public readonly ChunkData? PosX;
        public readonly ChunkData? NegX;
        public readonly ChunkData? PosZ;
        public readonly ChunkData? NegZ;
        public readonly bool TreatMissingAsSolid;

        public NeighborChunks(ChunkData? posX, ChunkData? negX, ChunkData? posZ, ChunkData? negZ, bool treatMissingAsSolid)
        {
            PosX = posX;
            NegX = negX;
            PosZ = posZ;
            NegZ = negZ;
            TreatMissingAsSolid = treatMissingAsSolid;
        }
    }

    public static MeshBuildData Build(ChunkData chunk, NeighborChunks neighbors)
    {
        int estimatedFaces = chunk.Size * chunk.Size * 8;
        List<Vector3> vertices = new(estimatedFaces * 4);
        List<Vector3> normals = new(estimatedFaces * 4);
        List<Color> colors = new(estimatedFaces * 4);
        List<int> indices = new(estimatedFaces * 6);

        for (int y = 0; y < chunk.Height; y++)
        {
            for (int z = 0; z < chunk.Size; z++)
            {
                for (int x = 0; x < chunk.Size; x++)
                {
                    BlockType type = chunk.Get(x, y, z);
                    if (type == BlockType.Air)
                    {
                        continue;
                    }

                    Color color = type switch
                    {
                        BlockType.Grass => new Color(0.32f, 0.75f, 0.25f),
                        BlockType.Tnt => new Color(0.9f, 0.1f, 0.1f),
                        _ => new Color(0.45f, 0.34f, 0.22f),
                    };

                    for (int face = 0; face < 6; face++)
                    {
                        Vector3I n = NeighborOffsets[face];
                        if (IsNeighborSolid(chunk, neighbors, x + n.X, y + n.Y, z + n.Z))
                        {
                            continue;
                        }

                        int start = vertices.Count;
                        Vector3[] faceVerts = FaceVertices[face];
                        Vector3 normal = FaceNormals[face];

                        vertices.Add(faceVerts[0] + new Vector3(x, y, z));
                        vertices.Add(faceVerts[1] + new Vector3(x, y, z));
                        vertices.Add(faceVerts[2] + new Vector3(x, y, z));
                        vertices.Add(faceVerts[3] + new Vector3(x, y, z));

                        normals.Add(normal);
                        normals.Add(normal);
                        normals.Add(normal);
                        normals.Add(normal);

                        colors.Add(color);
                        colors.Add(color);
                        colors.Add(color);
                        colors.Add(color);

                        indices.Add(start);
                        indices.Add(start + 2);
                        indices.Add(start + 1);
                        indices.Add(start);
                        indices.Add(start + 3);
                        indices.Add(start + 2);
                    }
                }
            }
        }

        return new MeshBuildData(vertices.ToArray(), normals.ToArray(), colors.ToArray(), indices.ToArray());
    }

    private static bool IsNeighborSolid(ChunkData chunk, NeighborChunks neighbors, int x, int y, int z)
    {
        if ((uint)y >= (uint)chunk.Height)
        {
            return false;
        }

        if ((uint)x < (uint)chunk.Size && (uint)z < (uint)chunk.Size)
        {
            return chunk.IsSolid(x, y, z);
        }

        if (x < 0)
        {
            return IsSolidInChunk(neighbors.NegX, x + chunk.Size, y, z, neighbors.TreatMissingAsSolid);
        }

        if (x >= chunk.Size)
        {
            return IsSolidInChunk(neighbors.PosX, x - chunk.Size, y, z, neighbors.TreatMissingAsSolid);
        }

        if (z < 0)
        {
            return IsSolidInChunk(neighbors.NegZ, x, y, z + chunk.Size, neighbors.TreatMissingAsSolid);
        }

        if (z >= chunk.Size)
        {
            return IsSolidInChunk(neighbors.PosZ, x, y, z - chunk.Size, neighbors.TreatMissingAsSolid);
        }

        return false;
    }

    private static bool IsSolidInChunk(ChunkData? chunk, int x, int y, int z, bool treatMissingAsSolid)
    {
        if (chunk is null)
        {
            return treatMissingAsSolid;
        }

        return chunk.IsSolid(x, y, z);
    }
}
