using Godot;

namespace justonlytnt.Gameplay;

public sealed partial class TntSystem
{
	private TntPrimedBody AcquireTnt()
	{
		if (_tntPool.Count == 0)
		{
			return CreateTntBody();
		}

		return _tntPool.Dequeue();
	}

	private void RecycleTnt(TntPrimedBody body)
	{
		body.Deactivate();
		_tntPool.Enqueue(body);
	}

	private DebrisBody AcquireDebris()
	{
		if (_debrisPool.Count == 0)
		{
			return CreateDebrisBody();
		}

		return _debrisPool.Dequeue();
	}

	private void RecycleDebris(DebrisBody body)
	{
		body.Deactivate();
		_debrisPool.Enqueue(body);
	}

	private TntPrimedBody CreateTntBody()
	{
		TntPrimedBody body = new();
		AddChild(body);
		return body;
	}

	private DebrisBody CreateDebrisBody()
	{
		DebrisBody body = new();
		AddChild(body);
		return body;
	}

	private struct ActiveTnt
	{
		public TntPrimedBody Body;
		public float Remaining;

		public ActiveTnt(TntPrimedBody body, float remaining)
		{
			Body = body;
			Remaining = remaining;
		}
	}

	private struct ActiveDebris
	{
		public DebrisBody Body;
		public float Remaining;

		public ActiveDebris(DebrisBody body, float remaining)
		{
			Body = body;
			Remaining = remaining;
		}
	}
}
