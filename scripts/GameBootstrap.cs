using Godot;
using justonlytnt.Gameplay;
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

	public static void QueueNextWorld(WorldConfig config, PlayerRuntimeSettings playerSettings)
	{
		_queuedWorldConfig = CloneConfig(config);
		_queuedPlayerSettings = playerSettings;
	}

	public override void _Ready()
	{
		WorldConfig config = _queuedWorldConfig is not null
			? CloneConfig(_queuedWorldConfig)
			: CreateDefaultConfig();

		PlayerRuntimeSettings? queuedPlayerSettings = _queuedPlayerSettings;
		_queuedWorldConfig = null;
		_queuedPlayerSettings = null;

		VoxelWorld world = new()
		{
			Name = "VoxelWorld",
			Config = config,
		};
		AddChild(world);

		TntSystem tnt = new()
		{
			Name = "TntSystem",
			ProcessPriority = -1,
		};
		AddChild(tnt);
		tnt.Setup(world);

		PlayerController player = new()
		{
			Name = "Player",
			Position = new Vector3(0, 42, 0),
		};
		player.Setup(world, tnt);
		if (queuedPlayerSettings.HasValue)
		{
			player.ApplyRuntimeSettings(queuedPlayerSettings.Value);
		}

		AddChild(player);
		world.PlayerTarget = player;
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
			MaxExplosionsPerFrame = 64,
			MaxChainSpawnsPerFrame = 64 * 4,
			MaxActivePrimedTnt = 512,
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
