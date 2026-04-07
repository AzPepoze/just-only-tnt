using Godot;
using justonlytnt.Gameplay;
using justonlytnt.Player;
using justonlytnt.World;

namespace justonlytnt;

public sealed partial class GameBootstrap : Node3D
{
	public override void _Ready()
	{
		WorldConfig config = new()
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
			TntFuseSeconds = 2.0f,
			TntBlastRadius = 4.5f,
			TntExplosionImpulse = 30.0f,
			TntChainFuseMinSeconds = 0.12f,
			TntChainFuseMaxSeconds = 0.7f,
			DebrisMaxPerExplosion = 80,
			DebrisLifetimeSeconds = 2.5f,
			DebrisImpulse = 16.0f,
			InteractionDistance = 10.0f,
		};

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
		AddChild(player);

		player.Setup(world, tnt);
		world.PlayerTarget = player;

		DirectionalLight3D sun = new()
		{
			RotationDegrees = new Vector3(-50f, -20f, 0f),
			LightEnergy = 1.2f,
			ShadowEnabled = true,
		};
		AddChild(sun);

		WorldEnvironment environment = new();
		Environment env = new()
		{
			BackgroundMode = Environment.BGMode.Sky,
			AmbientLightSource = Environment.AmbientSource.Sky,
			TonemapMode = Environment.ToneMapper.Aces,
		};
		Sky sky = new();
		sky.SkyMaterial = new ProceduralSkyMaterial();
		env.Sky = sky;
		environment.Environment = env;
		AddChild(environment);
	}
}
