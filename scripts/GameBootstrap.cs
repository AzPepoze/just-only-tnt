using Godot;
using justonlytnt.Gameplay;
using justonlytnt.Networking;
using justonlytnt.Player;
using justonlytnt.World;

namespace justonlytnt;

public sealed partial class GameBootstrap : Node3D
{
	public readonly struct PlayerRuntimeSettings
	{
		public readonly float WalkSpeed;
		public readonly float SprintSpeed;
		public readonly float JumpVelocity;
		public readonly float MouseSensitivity;
		public readonly float FlySpeed;
		public readonly float FlySprintSpeed;
		public readonly float FillHoldSeconds;

		public PlayerRuntimeSettings(
			float walkSpeed,
			float sprintSpeed,
			float jumpVelocity,
			float mouseSensitivity,
			float flySpeed,
			float flySprintSpeed,
			float fillHoldSeconds)
		{
			WalkSpeed = walkSpeed;
			SprintSpeed = sprintSpeed;
			JumpVelocity = jumpVelocity;
			MouseSensitivity = mouseSensitivity;
			FlySpeed = flySpeed;
			FlySprintSpeed = flySprintSpeed;
			FillHoldSeconds = fillHoldSeconds;
		}
	}

	private static WorldConfig? _queuedWorldConfig;
	private static PlayerRuntimeSettings? _queuedPlayerSettings;
	private VoxelWorld? _world;
	private TntSystem? _tnt;
	private PlayerController? _localPlayer;
	private Node3D? _serverStreamTarget;
	private bool _networkEnabled;
	private bool _dedicatedServer;

	public static void QueueNextWorld(WorldConfig config, PlayerRuntimeSettings playerSettings)
	{
		_queuedWorldConfig = CloneConfig(config);
		_queuedPlayerSettings = playerSettings;
	}

	public override void _Ready()
	{
		LaunchOptions.ParseCliOnce();
		LaunchMode mode = LaunchOptions.Mode;
		_dedicatedServer = mode == LaunchMode.DedicatedServer;
		_networkEnabled = mode is LaunchMode.Host or LaunchMode.Client or LaunchMode.DedicatedServer;
		SetupMultiplayer(mode);

		WorldConfig config = _queuedWorldConfig is not null
			? CloneConfig(_queuedWorldConfig)
			: CreateDefaultConfig();
		if (LaunchOptions.OverrideSeed.HasValue)
		{
			config.Seed = LaunchOptions.OverrideSeed.Value;
		}

		PlayerRuntimeSettings? queuedPlayerSettings = _queuedPlayerSettings;
		_queuedWorldConfig = null;
		_queuedPlayerSettings = null;

		_world = new VoxelWorld
		{
			Name = "VoxelWorld",
			Config = config,
		};
		AddChild(_world);

		_tnt = new TntSystem
		{
			Name = "TntSystem",
			ProcessPriority = -1,
		};
		AddChild(_tnt);
		_tnt.Setup(_world);
		_tnt.ConfigureNetworking(_networkEnabled, _dedicatedServer);

		if (!_dedicatedServer)
		{
			_localPlayer = new PlayerController
			{
				Name = "Player",
				Position = new Vector3(0, 42, 0),
			};
			_localPlayer.Setup(_world, _tnt);
			if (queuedPlayerSettings.HasValue)
			{
				_localPlayer.ApplyRuntimeSettings(queuedPlayerSettings.Value);
			}

			AddChild(_localPlayer);
			_world.PlayerTarget = mode == LaunchMode.Client ? null : _localPlayer;
		}
		else
		{
			_serverStreamTarget = new Node3D { Name = "ServerStreamTarget" };
			AddChild(_serverStreamTarget);
			_world.PlayerTarget = _serverStreamTarget;
		}

		InitializeNetworkRuntime(mode);
	}

	private static WorldConfig CreateDefaultConfig()
	{
		return new WorldConfig
		{
			Seed = 1337,
			ChunkSize = 16,
			ChunkHeight = 96,
			ViewDistanceChunks = 8,
			WorkerCount = System.Math.Max(2, System.Environment.ProcessorCount - 1),
			MaxMainThreadAppliesPerFrame = 6,
			MaxCollisionBuildsPerFrame = 3,
			MaxChunkCreatesPerFrame = 2,
			MaxChunkRemovalsPerFrame = 3,
			VisibilityRefreshSeconds = 0.08f,
			BaseTerrainHeight = 28,
			TerrainAmplitude = 20,
			TerrainFrequency = 0.035f,
			FillBlocksPerFrame = 20000,
			MaxExplosionsPerFrame = 256,
			MaxChainSpawnsPerFrame = 256 * 4,
			MaxActivePrimedTnt = 256 * 4,
			MaxDebrisSpawnsPerFrame = 32,
			TntFuseSeconds = 2.0f,
			TntBlastRadius = 4.5f,
			TntExplosionImpulse = 30.0f,
			TntChainFuseMinSeconds = 0.12f,
			TntChainFuseMaxSeconds = 0.7f,
			DebrisMaxPerExplosion = 80,
			DebrisLifetimeSeconds = 2.5f,
			DebrisImpulse = 16.0f,
			SpawnDebrisEnabled = true,
			InteractionDistance = 10.0f,
		};
	}

	private static WorldConfig CloneConfig(WorldConfig source)
	{
		return new WorldConfig
		{
			Seed = source.Seed,
			ChunkSize = source.ChunkSize,
			ChunkHeight = source.ChunkHeight,
			ViewDistanceChunks = source.ViewDistanceChunks,
			WorkerCount = source.WorkerCount,
			MaxMainThreadAppliesPerFrame = source.MaxMainThreadAppliesPerFrame,
			MaxCollisionBuildsPerFrame = source.MaxCollisionBuildsPerFrame,
			MaxChunkCreatesPerFrame = source.MaxChunkCreatesPerFrame,
			MaxChunkRemovalsPerFrame = source.MaxChunkRemovalsPerFrame,
			VisibilityRefreshSeconds = source.VisibilityRefreshSeconds,
			BaseTerrainHeight = source.BaseTerrainHeight,
			TerrainAmplitude = source.TerrainAmplitude,
			TerrainFrequency = source.TerrainFrequency,
			FillBlocksPerFrame = source.FillBlocksPerFrame,
			MaxExplosionsPerFrame = source.MaxExplosionsPerFrame,
			MaxChainSpawnsPerFrame = source.MaxChainSpawnsPerFrame,
			MaxActivePrimedTnt = source.MaxActivePrimedTnt,
			MaxDebrisSpawnsPerFrame = source.MaxDebrisSpawnsPerFrame,
			TntFuseSeconds = source.TntFuseSeconds,
			TntBlastRadius = source.TntBlastRadius,
			TntExplosionImpulse = source.TntExplosionImpulse,
			TntChainFuseMinSeconds = source.TntChainFuseMinSeconds,
			TntChainFuseMaxSeconds = source.TntChainFuseMaxSeconds,
			DebrisMaxPerExplosion = source.DebrisMaxPerExplosion,
			DebrisLifetimeSeconds = source.DebrisLifetimeSeconds,
			DebrisImpulse = source.DebrisImpulse,
			SpawnDebrisEnabled = source.SpawnDebrisEnabled,
			InteractionDistance = source.InteractionDistance,
		};
	}
}
