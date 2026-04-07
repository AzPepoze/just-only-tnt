using Godot;

namespace justonlytnt.Gameplay;

public sealed partial class TntPrimedBody : RigidBody3D
{
	private static readonly StandardMaterial3D Material = new()
	{
		AlbedoColor = new Color(0.95f, 0.2f, 0.2f),
		Roughness = 0.85f,
		Metallic = 0f,
	};

	private readonly MeshInstance3D _mesh = new();
	private readonly CollisionShape3D _collision = new();

	public override void _Ready()
	{
		BoxMesh box = new() { Size = Vector3.One * 0.9f };
		_mesh.Mesh = box;
		_mesh.MaterialOverride = Material;
		AddChild(_mesh);

		BoxShape3D shape = new() { Size = Vector3.One * 0.9f };
		_collision.Shape = shape;
		AddChild(_collision);

		ContinuousCd = true;
		GravityScale = 1.0f;
		Mass = 0.8f;
	}

	public void Activate(Vector3 position, Vector3 initialVelocity)
	{
		GlobalPosition = position;
		Visible = true;
		Freeze = false;
		Sleeping = false;
		LinearVelocity = initialVelocity;
		AngularVelocity = new Vector3(3.5f, 5.5f, 2.4f);
	}

	public void Deactivate()
	{
		Freeze = true;
		Sleeping = true;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		Visible = false;
		GlobalPosition = new Vector3(0f, -2000f, 0f);
	}
}
