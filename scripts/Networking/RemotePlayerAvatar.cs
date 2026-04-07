using Godot;

namespace justonlytnt.Networking;

public sealed partial class RemotePlayerAvatar : Node3D
{
    private readonly MeshInstance3D _mesh = new();
    private readonly StaticBody3D _collisionBody = new();
    private readonly CollisionShape3D _collisionShape = new();
    private readonly Label3D _label = new();
    private Vector3 _targetPosition;
    private float _targetYaw;

    public override void _Ready()
    {
        CapsuleMesh capsule = new()
        {
            Radius = 0.4f,
            Height = 1.2f,
        };

        StandardMaterial3D mat = new()
        {
            AlbedoColor = new Color(0.2f, 0.75f, 0.95f, 1f),
            Roughness = 0.9f,
            Metallic = 0f,
        };

        _mesh.Mesh = capsule;
        _mesh.MaterialOverride = mat;
        _mesh.Position = new Vector3(0f, 0.9f, 0f);
        AddChild(_mesh);

        _collisionBody.CollisionLayer = 0;
        _collisionBody.CollisionMask = 0;
        CapsuleShape3D colliderShape = new()
        {
            Radius = 0.4f,
            Height = 1.2f,
        };
        _collisionShape.Shape = colliderShape;
        _collisionShape.Position = new Vector3(0f, 0.9f, 0f);
        _collisionBody.AddChild(_collisionShape);
        AddChild(_collisionBody);

        _label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _label.Text = "Player";
        _label.Position = new Vector3(0f, 2.0f, 0f);
        _label.Modulate = new Color(1f, 1f, 1f, 0.95f);
        AddChild(_label);

        _targetPosition = GlobalPosition;
        _targetYaw = Rotation.Y;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        GlobalPosition = GlobalPosition.Lerp(_targetPosition, Mathf.Clamp(dt * 12f, 0f, 1f));

        float yaw = Mathf.LerpAngle(Rotation.Y, _targetYaw, Mathf.Clamp(dt * 12f, 0f, 1f));
        Rotation = new Vector3(0f, yaw, 0f);
    }

    public void SetDisplayName(string name)
    {
        _label.Text = string.IsNullOrWhiteSpace(name) ? "Player" : name;
    }

    public void SetNetworkPose(Vector3 position, float yaw)
    {
        _targetPosition = position;
        _targetYaw = yaw;
    }
}
