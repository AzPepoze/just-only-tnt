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

	private readonly Queue<DebrisBody> _debrisPool = new();
	private readonly List<ActiveDebris> _activeDebris = new();

	private readonly RandomNumberGenerator _rng = new();

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
			Explode(explosionCenter);
		}

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

		Vector3I min = new(
			Mathf.Min(startInclusive.X, endInclusive.X),
			Mathf.Min(startInclusive.Y, endInclusive.Y),
			Mathf.Min(startInclusive.Z, endInclusive.Z));

		Vector3I max = new(
			Mathf.Max(startInclusive.X, endInclusive.X),
			Mathf.Max(startInclusive.Y, endInclusive.Y),
			Mathf.Max(startInclusive.Z, endInclusive.Z));

		int placedCount = 0;
		for (int y = min.Y; y <= max.Y; y++)
		{
			for (int z = min.Z; z <= max.Z; z++)
			{
				for (int x = min.X; x <= max.X; x++)
				{
					if (OverwriteWithTnt(new Vector3I(x, y, z)))
					{
						placedCount++;
					}
				}
			}
		}

		return placedCount;
	}

	private bool OverwriteWithTnt(Vector3I position)
	{
		if (_world is null)
		{
			return false;
		}

		_world.SetBlock(position, BlockType.Tnt);
		return _world.GetBlock(position) == BlockType.Tnt;
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
}
