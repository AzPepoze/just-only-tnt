using Godot;
using justonlytnt.World;

namespace justonlytnt.Gameplay;

public sealed partial class DebrisBody : RigidBody3D
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
	private readonly CollisionShape3D _collision = new();

	public override void _Ready()
	{
		BoxMesh box = new() { Size = Vector3.One * 0.45f };
		_mesh.Mesh = box;
		_mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
		AddChild(_mesh);

		BoxShape3D shape = new() { Size = Vector3.One * 0.45f };
		_collision.Shape = shape;
		AddChild(_collision);

		ContinuousCd = true;
		GravityScale = 1.0f;
		Mass = 0.25f;
	}

	public void Activate(Vector3 position, Vector3 velocity, BlockType type)
	{
		GlobalPosition = position;
		Visible = true;
		Freeze = false;
		Sleeping = false;
		LinearVelocity = velocity;
		AngularVelocity = velocity.Cross(Vector3.Up).Normalized() * 8.0f;

		_mesh.MaterialOverride = type == BlockType.Grass ? GrassMaterial : DirtMaterial;
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
