using Godot;
using justonlytnt;
using justonlytnt.Gameplay;
using justonlytnt.World;

namespace justonlytnt.Player;

public sealed partial class PlayerController : CharacterBody3D
{
    private enum HotbarItem
    {
        Tnt = 0,
        FlintAndSteel = 1,
    }

    [Export] public float WalkSpeed { get; set; } = 10f;
    [Export] public float SprintSpeed { get; set; } = 14f;
    [Export] public float JumpVelocity { get; set; } = 8.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.0022f;
    [Export] public float GroundAcceleration { get; set; } = 55f;
    [Export] public float AirAcceleration { get; set; } = 18f;
    [Export] public float GroundDeceleration { get; set; } = 40f;
    [Export] public float FlySpeed { get; set; } = 12f;
    [Export] public float FlySprintSpeed { get; set; } = 20f;
    [Export] public float FlyVerticalSpeed { get; set; } = 10f;
    [Export] public float DoubleTapWindowSeconds { get; set; } = 0.28f;
    [Export] public float InitialGroundLockSeconds { get; set; } = 2.5f;
    [Export(PropertyHint.Range, "0.1,1.2,0.05")] public float FillHoldSeconds { get; set; } = 0.3f;

    private VoxelWorld? _world;
    private TntSystem? _tnt;
    private Camera3D? _camera;

    private float _pitch;
    private bool _isFlying;
    private double _lastJumpPressedTime = -10.0;
    private bool _initialGroundLockActive = true;
    private float _initialGroundLockTimer;
    private Vector2 _cachedMoveInput;
    private bool _cachedJumpHeld;
    private bool _cachedDescendHeld;
    private bool _cachedSprintHeld;
    private bool _cachedPlaceHeld;
    private bool _jumpPressedQueued;
    private bool _placePressedQueued;
    private bool _placeReleasedQueued;
    private bool _ignitePressedQueued;
    private HotbarItem _selectedItem = HotbarItem.Tnt;
    private bool _fillPlacementStarted;
    private bool _fillModeActive;
    private float _fillHoldTimer;
    private Vector3I _fillStartBlock;
    private Vector3I _fillEndBlock;
    private MeshInstance3D? _fillPreview;
    private BoxMesh? _fillPreviewMesh;

    private PanelContainer? _slot1Panel;
    private PanelContainer? _slot2Panel;
    private Label? _slot1Label;
    private Label? _slot2Label;
    private CanvasLayer? _optionsLayer;
    private bool _optionsMenuOpen;
    public float ViewPitch => _pitch;

    public void Setup(VoxelWorld world, TntSystem tnt)
    {
        _world = world;
        _tnt = tnt;
    }

    public override void _Ready()
    {
        EnsureInputActions();
        BuildCollisionAndCamera();
        BuildFillPreview();
        BuildOptionsMenu();
        BuildHotbarUi();
        UpdateHotbarUi();
        BuildDebugOverlay();
        CacheHardwareInfo();

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public void ApplyRuntimeSettings(GameBootstrap.PlayerRuntimeSettings settings)
    {
        WalkSpeed = settings.WalkSpeed;
        SprintSpeed = settings.SprintSpeed;
        JumpVelocity = settings.JumpVelocity;
        MouseSensitivity = settings.MouseSensitivity;
        FlySpeed = settings.FlySpeed;
        FlySprintSpeed = settings.FlySprintSpeed;
        FillHoldSeconds = settings.FillHoldSeconds;
    }

    private void BuildCollisionAndCamera()
    {
        CollisionShape3D collision = new();
        CapsuleShape3D shape = new()
        {
            Radius = 0.4f,
            Height = 1.2f,
        };
        collision.Shape = shape;
        AddChild(collision);

        _camera = new Camera3D
        {
            Position = new Vector3(0, 0.75f, 0),
            Current = true,
        };
        AddChild(_camera);
    }
}
