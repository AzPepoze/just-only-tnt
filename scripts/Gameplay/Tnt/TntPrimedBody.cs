using Godot;

namespace justonlytnt.Gameplay;

public sealed partial class TntPrimedBody : Node3D
{
	private static readonly StandardMaterial3D Material = new()
	{
		AlbedoColor = new Color(0.95f, 0.2f, 0.2f),
		Roughness = 0.85f,
		Metallic = 0f,
	};

	private readonly MeshInstance3D _mesh = new();

	public override void _Ready()
	{
		BoxMesh box = new() { Size = Vector3.One * 0.9f };
		_mesh.Mesh = box;
		_mesh.MaterialOverride = Material;
		AddChild(_mesh);
	}

	public void Activate(Vector3 position)
	{
		GlobalPosition = position;
		Visible = true;
	}

	public void SetSimulatedPosition(Vector3 position) => GlobalPosition = position;

	public void Deactivate()
	{
		Visible = false;
		GlobalPosition = new Vector3(0f, -2000f, 0f);
	}
}
