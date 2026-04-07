using System.Collections.Generic;
using Godot;
using justonlytnt.World;

namespace justonlytnt.Gameplay;

public sealed partial class TntSystem : Node3D
{
	private const float TntHalfHeight = 0.45f;
	private const float DebrisHalfHeight = 0.225f;

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
	private readonly List<TntPrimedBody> _clientVisualTnt = new();

	private readonly RandomNumberGenerator _rng = new();
	private bool _networked;
	private bool _dedicatedServer;
	private double _snapshotAccumulator;

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

	public void ConfigureNetworking(bool enabled, bool dedicatedServer)
	{
		_networked = enabled;
		_dedicatedServer = dedicatedServer;
	}

	public override void _Process(double delta)
	{
		if (_world is null || _config is null)
		{
			return;
		}

		float dt = (float)delta;

		if (_networked && !Multiplayer.IsServer())
		{
			ProcessPendingDebrisSpawns();
			SimulateActiveDebris(dt);
			return;
		}

		for (int i = _activeTnt.Count - 1; i >= 0; i--)
		{
			ActiveTnt active = _activeTnt[i];
			active.Remaining -= dt;
			SimulateBlockOnlyProjectile(
				ref active.Position,
				ref active.Velocity,
				dt,
				TntHalfHeight,
				bounce: 0f,
				friction: 0.72f);
			active.Body?.SetSimulatedPosition(active.Position);

			if (active.Remaining > 0f)
			{
				_activeTnt[i] = active;
				continue;
			}

			Vector3 explosionCenter = active.Position;
			if (active.Body is not null)
			{
				RecycleTnt(active.Body);
			}

			_activeTnt.RemoveAt(i);
			_pendingExplosionQueue.Enqueue(explosionCenter);
		}

		ProcessPendingExplosions();
		ProcessPendingChainSpawns();
		ProcessPendingDebrisSpawns();
		SimulateActiveDebris(dt);

		if (_networked && Multiplayer.IsServer())
		{
			_snapshotAccumulator += delta;
			if (_snapshotAccumulator >= 0.05)
			{
				_snapshotAccumulator = 0.0;
				BroadcastTntSnapshot();
			}
		}
	}

	public bool PlaceTnt(Vector3I targetBlock, Vector3 normal)
	{
		if (_networked && !Multiplayer.IsServer())
		{
			RpcId(1, nameof(ServerRequestPlace), targetBlock, normal);
			return true;
		}

		Vector3I placeAt = targetBlock + new Vector3I(
			Mathf.RoundToInt(normal.X),
			Mathf.RoundToInt(normal.Y),
			Mathf.RoundToInt(normal.Z));

		bool placed = PlaceTntAtInternal(placeAt);
		if (placed && _networked && Multiplayer.IsServer())
		{
			Rpc(nameof(ClientSetBlock), placeAt, (int)BlockType.Tnt);
		}

		return placed;
	}

	public bool PlaceTntAt(Vector3I placeAt)
	{
		if (_networked && !Multiplayer.IsServer())
		{
			RpcId(1, nameof(ServerRequestPlaceAt), placeAt);
			return true;
		}

		bool placed = PlaceTntAtInternal(placeAt);
		if (placed && _networked && Multiplayer.IsServer())
		{
			Rpc(nameof(ClientSetBlock), placeAt, (int)BlockType.Tnt);
		}

		return placed;
	}

	public int FillTntBox(Vector3I startInclusive, Vector3I endInclusive)
	{
		if (_networked && !Multiplayer.IsServer())
		{
			RpcId(1, nameof(ServerRequestFill), startInclusive, endInclusive);
			return 0;
		}

		int queued = FillTntBoxInternal(startInclusive, endInclusive);
		if (_networked && Multiplayer.IsServer())
		{
			Rpc(nameof(ClientQueueFill), startInclusive, endInclusive);
		}

		return queued;
	}

	public bool IgniteTnt(Vector3I targetBlock)
	{
		if (_config is null)
		{
			return false;
		}

		if (_networked && !Multiplayer.IsServer())
		{
			RpcId(1, nameof(ServerRequestIgnite), targetBlock);
			return true;
		}

		bool ignited = IgniteTntInternal(targetBlock, _config.TntFuseSeconds, Vector3.Up * 2.0f);
		if (ignited && _networked && Multiplayer.IsServer())
		{
			Rpc(nameof(ClientSetBlock), targetBlock, (int)BlockType.Air);
		}

		return ignited;
	}

	public bool IgniteTnt(Vector3I targetBlock, float fuseSeconds, Vector3 initialVelocity)
	{
		if (_networked && !Multiplayer.IsServer())
		{
			RpcId(1, nameof(ServerRequestIgnite), targetBlock);
			return true;
		}

		bool ignited = IgniteTntInternal(targetBlock, fuseSeconds, initialVelocity);
		if (ignited && _networked && Multiplayer.IsServer())
		{
			Rpc(nameof(ClientSetBlock), targetBlock, (int)BlockType.Air);
		}

		return ignited;
	}

	private bool PlaceTntAtInternal(Vector3I placeAt)
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

	private int FillTntBoxInternal(Vector3I startInclusive, Vector3I endInclusive)
	{
		if (_world is null)
		{
			return 0;
		}

		long queued = _world.QueueOverwriteFill(startInclusive, endInclusive, BlockType.Tnt);
		return queued > int.MaxValue ? int.MaxValue : (int)queued;
	}

	private bool IgniteTntInternal(Vector3I targetBlock, float fuseSeconds, Vector3 initialVelocity)
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
		Vector3 spawn = new(blockPosition.X + 0.5f, blockPosition.Y + 0.5f, blockPosition.Z + 0.5f);
		TntPrimedBody? body = null;
		if (!_dedicatedServer)
		{
			body = AcquireTnt();
			body.Activate(spawn);
		}

		_activeTnt.Add(new ActiveTnt(body, spawn, initialVelocity, Mathf.Max(0.05f, fuseSeconds)));
	}

	private void SimulateBlockOnlyProjectile(
		ref Vector3 position,
		ref Vector3 velocity,
		float dt,
		float halfHeight,
		float bounce,
		float friction)
	{
		if (_world is null)
		{
			position += velocity * dt;
			return;
		}

		velocity += Vector3.Down * 18f * dt;
		Vector3 next = position + (velocity * dt);

		if (next.Y < halfHeight)
		{
			next.Y = halfHeight;
			if (velocity.Y < 0f)
			{
				velocity.Y = -velocity.Y * bounce;
			}
			velocity.X *= friction;
			velocity.Z *= friction;
		}
		else
		{
			float probeY = next.Y - halfHeight - 0.001f;
			Vector3I supportBlock = new(
				Mathf.FloorToInt(next.X),
				Mathf.FloorToInt(probeY),
				Mathf.FloorToInt(next.Z));

			if (_world.GetBlock(supportBlock) != BlockType.Air && velocity.Y <= 0f)
			{
				next.Y = supportBlock.Y + 1f + halfHeight + 0.001f;
				velocity.Y = -velocity.Y * bounce;
				velocity.X *= friction;
				velocity.Z *= friction;
			}
		}

		Vector3I block = new(Mathf.FloorToInt(next.X), Mathf.FloorToInt(next.Y), Mathf.FloorToInt(next.Z));
		if (_world.GetBlock(block) != BlockType.Air)
		{
				if (velocity.Y < 0f)
				{
					next.Y = block.Y + 1f + halfHeight + 0.01f;
					velocity.Y = -velocity.Y * bounce;
				}
				else
				{
					next = position;
					velocity.X *= 0.45f;
					velocity.Z *= 0.45f;
					velocity.Y = Mathf.Min(velocity.Y, 0f);
				}

			velocity.X *= friction;
			velocity.Z *= friction;
		}

		position = next;
	}

	private void SimulateActiveDebris(float dt)
	{
		for (int i = _activeDebris.Count - 1; i >= 0; i--)
		{
			ActiveDebris debris = _activeDebris[i];
			debris.Remaining -= dt;
			SimulateBlockOnlyProjectile(
				ref debris.Position,
				ref debris.Velocity,
				dt,
				DebrisHalfHeight,
				bounce: 0f,
				friction: 0.55f);
			debris.Body.SetSimulatedPosition(debris.Position);

			if (debris.Remaining > 0f)
			{
				_activeDebris[i] = debris;
				continue;
			}

			RecycleDebris(debris.Body);
			_activeDebris.RemoveAt(i);
		}
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
			body.Activate(pending.Position, pending.Type);
			_activeDebris.Add(new ActiveDebris(body, pending.Position, pending.Velocity, _config.DebrisLifetimeSeconds));
			budget--;
		}
	}

	private void BroadcastTntSnapshot()
	{
		Godot.Collections.Array<float> packed = new();
		for (int i = 0; i < _activeTnt.Count; i++)
		{
			Vector3 p = _activeTnt[i].Position;
			packed.Add(p.X);
			packed.Add(p.Y);
			packed.Add(p.Z);
		}

		Rpc(nameof(ClientReceiveTntSnapshot), packed);
	}

	private void SyncClientTntVisuals(Godot.Collections.Array<float> packed)
	{
		if (!_networked || Multiplayer.IsServer())
		{
			return;
		}

		int count = packed.Count / 3;
		while (_clientVisualTnt.Count < count)
		{
			TntPrimedBody body = AcquireTnt();
			body.Activate(new Vector3(0f, -2000f, 0f));
			_clientVisualTnt.Add(body);
		}

		for (int i = 0; i < count; i++)
		{
			Vector3 pos = new(packed[i * 3], packed[(i * 3) + 1], packed[(i * 3) + 2]);
			_clientVisualTnt[i].Activate(pos);
			_clientVisualTnt[i].SetSimulatedPosition(pos);
		}

		for (int i = _clientVisualTnt.Count - 1; i >= count; i--)
		{
			TntPrimedBody body = _clientVisualTnt[i];
			_clientVisualTnt.RemoveAt(i);
			RecycleTnt(body);
		}
	}

	private Godot.Collections.Array<int> SerializeRemovedBlocks(IReadOnlyList<RemovedBlock> removed)
	{
		Godot.Collections.Array<int> packed = new();
		for (int i = 0; i < removed.Count; i++)
		{
			RemovedBlock block = removed[i];
			packed.Add(block.Position.X);
			packed.Add(block.Position.Y);
			packed.Add(block.Position.Z);
			packed.Add((int)block.Type);
		}

		return packed;
	}

	private void BroadcastExplosionDelta(IReadOnlyList<RemovedBlock> removed, Vector3 center)
	{
		if (!_networked || !Multiplayer.IsServer())
		{
			return;
		}

		Godot.Collections.Array<int> packed = SerializeRemovedBlocks(removed);
		Rpc(nameof(ClientApplyExplosionDelta), packed, center);
	}

	private void SpawnClientDebrisFromExplosion(Godot.Collections.Array<int> packed, Vector3 explosionCenter)
	{
		if (_config is null || _config.DebrisMaxPerExplosion <= 0)
		{
			return;
		}

		int count = packed.Count / 4;
		if (count == 0)
		{
			return;
		}

		int spawnCount = Mathf.Min(count, _config.DebrisMaxPerExplosion);
		float step = count / (float)spawnCount;

		for (int i = 0; i < spawnCount; i++)
		{
			int index = Mathf.Clamp(Mathf.FloorToInt(i * step), 0, count - 1);
			int baseIndex = index * 4;
			Vector3 spawnPos = new(
				packed[baseIndex] + 0.5f,
				packed[baseIndex + 1] + 0.5f,
				packed[baseIndex + 2] + 0.5f);
			BlockType type = (BlockType)packed[baseIndex + 3];

			Vector3 offset = spawnPos - explosionCenter;
			float length = Mathf.Max(offset.Length(), 0.001f);
			Vector3 direction = (offset / length) + (Vector3.Up * 0.7f);
			float velocityScale = (_config.DebrisImpulse / (0.25f + length)) * _rng.RandfRange(0.8f, 1.3f);
			Vector3 velocity = direction.Normalized() * velocityScale;

			_pendingDebrisSpawns.Enqueue(new PendingDebrisSpawn(spawnPos, velocity, type));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void ServerRequestPlace(Vector3I targetBlock, Vector3 normal)
	{
		if (!_networked || !Multiplayer.IsServer())
		{
			return;
		}

		PlaceTnt(targetBlock, normal);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void ServerRequestPlaceAt(Vector3I placeAt)
	{
		if (!_networked || !Multiplayer.IsServer())
		{
			return;
		}

		PlaceTntAt(placeAt);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void ServerRequestFill(Vector3I startInclusive, Vector3I endInclusive)
	{
		if (!_networked || !Multiplayer.IsServer())
		{
			return;
		}

		FillTntBox(startInclusive, endInclusive);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void ServerRequestIgnite(Vector3I targetBlock)
	{
		if (!_networked || !Multiplayer.IsServer())
		{
			return;
		}

		IgniteTnt(targetBlock);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void ClientSetBlock(Vector3I position, int typeInt)
	{
		if (_world is null || (!_networked || Multiplayer.IsServer()))
		{
			return;
		}

		_world.SetBlock(position, (BlockType)typeInt);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void ClientQueueFill(Vector3I startInclusive, Vector3I endInclusive)
	{
		if (_world is null || (!_networked || Multiplayer.IsServer()))
		{
			return;
		}

		_world.QueueOverwriteFill(startInclusive, endInclusive, BlockType.Tnt);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void ClientApplyExplosionDelta(Godot.Collections.Array<int> packed, Vector3 explosionCenter)
	{
		if (_world is null || (!_networked || Multiplayer.IsServer()))
		{
			return;
		}

		_world.QueueSetBlocksFromPacked(packed, BlockType.Air, 4);
		if (_config is not null && _config.SpawnDebrisEnabled)
		{
			SpawnClientDebrisFromExplosion(packed, explosionCenter);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void ClientReceiveTntSnapshot(Godot.Collections.Array<float> packed)
	{
		SyncClientTntVisuals(packed);
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
