using Godot;

namespace justonlytnt.World;

public sealed partial class ChunkRenderer : Node3D
{
    private static readonly StandardMaterial3D SharedMaterial = new()
    {
        VertexColorUseAsAlbedo = true,
        Roughness = 1.0f,
        Metallic = 0.0f,
    };

    private readonly MeshInstance3D _meshInstance = new();
    private readonly StaticBody3D _staticBody = new();
    private readonly CollisionShape3D _collisionShape = new();

    public override void _Ready()
    {
        _meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        AddChild(_meshInstance);

        _staticBody.AddChild(_collisionShape);
        AddChild(_staticBody);
    }

    public void ApplyMesh(MeshBuildData data, bool updateCollision)
    {
        if (data.IsEmpty)
        {
            _meshInstance.Mesh = null;
            _collisionShape.Shape = null;
            return;
        }

        ArrayMesh mesh = new();
        Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.Vertices;
        arrays[(int)Mesh.ArrayType.Normal] = data.Normals;
        arrays[(int)Mesh.ArrayType.Color] = data.Colors;
        arrays[(int)Mesh.ArrayType.Index] = data.Indices;
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        mesh.SurfaceSetMaterial(0, SharedMaterial);

        _meshInstance.Mesh = mesh;

        if (updateCollision)
        {
            ConcavePolygonShape3D shape = new();
            Vector3[] faces = new Vector3[data.Indices.Length];
            for (int i = 0; i < data.Indices.Length; i++)
            {
                faces[i] = data.Vertices[data.Indices[i]];
            }

            shape.Data = faces;
            _collisionShape.Shape = shape;
        }
    }

    public void ResetForPool()
    {
        _meshInstance.Mesh = null;
        _collisionShape.Shape = null;
        Visible = false;
    }
}
