using Godot;

namespace justonlytnt.World;

public sealed class MeshBuildData
{
    public readonly Vector3[] Vertices;
    public readonly Vector3[] Normals;
    public readonly Color[] Colors;
    public readonly int[] Indices;

    public MeshBuildData(Vector3[] vertices, Vector3[] normals, Color[] colors, int[] indices)
    {
        Vertices = vertices;
        Normals = normals;
        Colors = colors;
        Indices = indices;
    }

    public bool IsEmpty => Vertices.Length == 0;
}
