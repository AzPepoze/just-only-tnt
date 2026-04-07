using Godot;
using justonlytnt.World;

namespace justonlytnt.Gameplay;

public sealed partial class DebrisBody : Node3D
{
	private static readonly StandardMaterial3D DirtMaterial = new()
	{
		AlbedoColor = new Color(0.45f, 0.34f, 0.22f),
		Roughness = 1.0f,
		Metallic = 0f,
	};

	private static readonly StandardMaterial3D GrassMaterial = new()
	{
		AlbedoColor = new Color(0.32f, 0.75f, 0.25f),
		Roughness = 1.0f,
		Metallic = 0f,
	};

	private readonly MeshInstance3D _mesh = new();

	public override void _Ready()
	{
		BoxMesh box = new() { Size = Vector3.One * 0.45f };
		_mesh.Mesh = box;
		_mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
		AddChild(_mesh);
	}

	public void Activate(Vector3 position, BlockType type)
	{
		GlobalPosition = position;
		Visible = true;
		_mesh.MaterialOverride = type == BlockType.Grass ? GrassMaterial : DirtMaterial;
	}

	public void SetSimulatedPosition(Vector3 position) => GlobalPosition = position;

	public void Deactivate()
	{
		Visible = false;
		GlobalPosition = new Vector3(0f, -2000f, 0f);
	}
}
