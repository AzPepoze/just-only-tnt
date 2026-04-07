using System.Collections.Generic;
using Godot;
using justonlytnt.World;

namespace justonlytnt.Gameplay;

public sealed partial class TntSystem : Node3D
{
	[Export] public int PrewarmCount { get; set; } = 128;
	[Export] public int DebrisPrewarmCount { get; set; } = 256;

	private VoxelWorld? _world;
	private WorldConfig? _config;

	private readonly Queue<TntPrimedBody> _tntPool = new();
	private readonly List<ActiveTnt> _activeTnt = new();
	private readonly Queue<Vector3> _pendingExplosionQueue = new();
	private readonly Queue<PendingChainSpawn> _pendingChainSpawns = new();
	private readonly Queue<PendingDebrisSpawn> _pendingDebrisSpawns = new();

	private readonly Queue<DebrisBody> _debrisPool = new();
	private readonly List<ActiveDebris> _activeDebris = new();

	private readonly RandomNumberGenerator _rng = new();

	public int ActivePrimedTntCount => _activeTnt.Count;
	public int PendingExplosionCount => _pendingExplosionQueue.Count;
	public int PendingChainSpawnCount => _pendingChainSpawns.Count;
	public int PendingDebrisSpawnCount => _pendingDebrisSpawns.Count;

	public void Setup(VoxelWorld world)
	{
		_world = world;
		_config = world.Config;
		_rng.Seed = (ulong)_config.Seed;

		for (int i = 0; i < PrewarmCount; i++)
		{
			TntPrimedBody body = CreateTntBody();
			body.Deactivate();
			_tntPool.Enqueue(body);
		}

		for (int i = 0; i < DebrisPrewarmCount; i++)
		{
			DebrisBody body = CreateDebrisBody();
			body.Deactivate();
			_debrisPool.Enqueue(body);
		}
	}

	public override void _Process(double delta)
	{
		if (_world is null || _config is null)
		{
			return;
		}

		float dt = (float)delta;

		for (int i = _activeTnt.Count - 1; i >= 0; i--)
		{
			ActiveTnt active = _activeTnt[i];
			active.Remaining -= dt;
			if (active.Remaining > 0f)
			{
				_activeTnt[i] = active;
				continue;
			}

			Vector3 explosionCenter = active.Body.GlobalPosition;
			RecycleTnt(active.Body);
			_activeTnt.RemoveAt(i);
			_pendingExplosionQueue.Enqueue(explosionCenter);
		}

		ProcessPendingExplosions();
		ProcessPendingChainSpawns();
		ProcessPendingDebrisSpawns();

		for (int i = _activeDebris.Count - 1; i >= 0; i--)
		{
			ActiveDebris debris = _activeDebris[i];
			debris.Remaining -= dt;
			if (debris.Remaining > 0f)
			{
				_activeDebris[i] = debris;
				continue;
			}

			RecycleDebris(debris.Body);
			_activeDebris.RemoveAt(i);
		}
	}

	public bool PlaceTnt(Vector3I targetBlock, Vector3 normal)
	{
		Vector3I placeAt = targetBlock + new Vector3I(
			Mathf.RoundToInt(normal.X),
			Mathf.RoundToInt(normal.Y),
			Mathf.RoundToInt(normal.Z));

		return PlaceTntAt(placeAt);
	}

	public bool PlaceTntAt(Vector3I placeAt)
	{
		if (_world is null)
		{
			return false;
		}

		if (_world.GetBlock(placeAt) != BlockType.Air)
		{
			return false;
		}

		_world.SetBlock(placeAt, BlockType.Tnt);
		return true;
	}

	public int FillTntBox(Vector3I startInclusive, Vector3I endInclusive)
	{
		if (_world is null)
		{
			return 0;
		}

		long queued = _world.QueueOverwriteFill(startInclusive, endInclusive, BlockType.Tnt);
		return queued > int.MaxValue ? int.MaxValue : (int)queued;
	}

	public bool IgniteTnt(Vector3I targetBlock)
	{
		if (_config is null)
		{
			return false;
		}

		return IgniteTnt(targetBlock, _config.TntFuseSeconds, Vector3.Up * 2.0f);
	}

	public bool IgniteTnt(Vector3I targetBlock, float fuseSeconds, Vector3 initialVelocity)
	{
		if (_world is null)
		{
			return false;
		}

		if (_world.GetBlock(targetBlock) != BlockType.Tnt)
		{
			return false;
		}

		_world.SetBlock(targetBlock, BlockType.Air);
		SpawnPrimedTnt(targetBlock, fuseSeconds, initialVelocity);
		return true;
	}

	private void SpawnPrimedTnt(Vector3I blockPosition, float fuseSeconds, Vector3 initialVelocity)
	{
		TntPrimedBody body = AcquireTnt();
		body.Activate(
			new Vector3(blockPosition.X + 0.5f, blockPosition.Y + 0.5f, blockPosition.Z + 0.5f),
			initialVelocity);

		_activeTnt.Add(new ActiveTnt(body, Mathf.Max(0.05f, fuseSeconds)));
	}

	private void ProcessPendingExplosions()
	{
		if (_config is null)
		{
			return;
		}

		int budget = Mathf.Max(1, _config.MaxExplosionsPerFrame);
		while (budget > 0 && _pendingExplosionQueue.Count > 0)
		{
			Vector3 center = _pendingExplosionQueue.Dequeue();
			Explode(center);
			budget--;
		}
	}

	private void ProcessPendingChainSpawns()
	{
		if (_config is null)
		{
			return;
		}

		int budget = Mathf.Max(1, _config.MaxChainSpawnsPerFrame);
		int maxActive = Mathf.Max(1, _config.MaxActivePrimedTnt);

		while (budget > 0 && _pendingChainSpawns.Count > 0)
		{
			if (_activeTnt.Count >= maxActive)
			{
				break;
			}

			PendingChainSpawn pending = _pendingChainSpawns.Dequeue();
			SpawnPrimedTnt(pending.Position, pending.FuseSeconds, pending.InitialVelocity);
			budget--;
		}
	}

	private void ProcessPendingDebrisSpawns()
	{
		if (_config is null)
		{
			return;
		}

		int budget = Mathf.Max(1, _config.MaxDebrisSpawnsPerFrame);
		while (budget > 0 && _pendingDebrisSpawns.Count > 0)
		{
			PendingDebrisSpawn pending = _pendingDebrisSpawns.Dequeue();
			DebrisBody body = AcquireDebris();
			body.Activate(pending.Position, pending.Velocity, pending.Type);
			_activeDebris.Add(new ActiveDebris(body, _config.DebrisLifetimeSeconds));
			budget--;
		}
	}

	private readonly struct PendingChainSpawn
	{
		public readonly Vector3I Position;
		public readonly float FuseSeconds;
		public readonly Vector3 InitialVelocity;

		public PendingChainSpawn(Vector3I position, float fuseSeconds, Vector3 initialVelocity)
		{
			Position = position;
			FuseSeconds = fuseSeconds;
			InitialVelocity = initialVelocity;
		}
	}

	private readonly struct PendingDebrisSpawn
	{
		public readonly Vector3 Position;
		public readonly Vector3 Velocity;
		public readonly BlockType Type;

		public PendingDebrisSpawn(Vector3 position, Vector3 velocity, BlockType type)
		{
			Position = position;
			Velocity = velocity;
			Type = type;
		}
	}
}
