using System.Collections.Generic;
using Godot;
using justonlytnt.World;

namespace justonlytnt.Gameplay;

public sealed partial class TntSystem
{
	private void Explode(Vector3 center)
	{
		if (_world is null || _config is null)
		{
			return;
		}

		ExplosionResult result = _world.ApplyExplosion(center, _config.TntBlastRadius, _config.TntExplosionImpulse);
		IgniteChainTnt(result, center);

		if (_config.SpawnDebrisEnabled)
		{
			SpawnDebris(result, center);
		}

		ApplyRadialImpulseToPrimedTnt(center, _config.TntBlastRadius, _config.TntExplosionImpulse);
	}

	private void IgniteChainTnt(ExplosionResult result, Vector3 explosionCenter)
	{
		if (_config is null)
		{
			return;
		}

		float minFuse = Mathf.Min(_config.TntChainFuseMinSeconds, _config.TntChainFuseMaxSeconds);
		float maxFuse = Mathf.Max(_config.TntChainFuseMinSeconds, _config.TntChainFuseMaxSeconds);

		foreach (RemovedBlock removed in result.RemovedBlocks)
		{
			if (removed.Type != BlockType.Tnt)
			{
				continue;
			}

			Vector3 spawnPosition = new Vector3(
				removed.Position.X + 0.5f,
				removed.Position.Y + 0.5f,
				removed.Position.Z + 0.5f);

			Vector3 offset = spawnPosition - explosionCenter;
			float length = Mathf.Max(offset.Length(), 0.001f);
			Vector3 direction = (offset / length) + (Vector3.Up * 0.65f);
			Vector3 chainVelocity = direction.Normalized() * _rng.RandfRange(2.5f, 6.0f);
			float fuse = _rng.RandfRange(minFuse, maxFuse);

			SpawnPrimedTnt(removed.Position, fuse, chainVelocity);
		}
	}

	private void SpawnDebris(ExplosionResult result, Vector3 explosionCenter)
	{
		if (_config is null || _config.DebrisMaxPerExplosion <= 0)
		{
			return;
		}

		List<RemovedBlock> candidates = new();
		foreach (RemovedBlock removed in result.RemovedBlocks)
		{
			if (removed.Type != BlockType.Air && removed.Type != BlockType.Tnt)
			{
				candidates.Add(removed);
			}
		}

		if (candidates.Count == 0)
		{
			return;
		}

		int spawnCount = Mathf.Min(_config.DebrisMaxPerExplosion, candidates.Count);
		float step = candidates.Count / (float)spawnCount;

		for (int i = 0; i < spawnCount; i++)
		{
			int index = Mathf.Clamp(Mathf.FloorToInt(i * step), 0, candidates.Count - 1);
			RemovedBlock removed = candidates[index];

			DebrisBody body = AcquireDebris();
			Vector3 spawnPosition = new Vector3(
				removed.Position.X + 0.5f,
				removed.Position.Y + 0.5f,
				removed.Position.Z + 0.5f);

			Vector3 offset = spawnPosition - explosionCenter;
			float length = Mathf.Max(offset.Length(), 0.001f);
			Vector3 direction = (offset / length) + (Vector3.Up * 0.7f);

			float velocityScale = (_config.DebrisImpulse / (0.25f + length)) * _rng.RandfRange(0.8f, 1.3f);
			Vector3 velocity = direction.Normalized() * velocityScale;

			body.Activate(spawnPosition, velocity, removed.Type);
			_activeDebris.Add(new ActiveDebris(body, _config.DebrisLifetimeSeconds));
		}
	}

	private void ApplyRadialImpulseToPrimedTnt(Vector3 center, float radius, float power)
	{
		for (int i = 0; i < _activeTnt.Count; i++)
		{
			TntPrimedBody body = _activeTnt[i].Body;
			Vector3 offset = body.GlobalPosition - center;
			float distance = offset.Length();
			if (distance > radius * 2.0f)
			{
				continue;
			}

			float attenuation = 1.0f / (0.2f + distance);
			Vector3 impulse = (offset.Normalized() + (Vector3.Up * 0.25f)) * (power * attenuation * 0.08f);
			body.ApplyCentralImpulse(impulse);
		}
	}
}
