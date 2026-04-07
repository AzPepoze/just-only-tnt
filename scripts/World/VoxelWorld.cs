using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using justonlytnt.World.Jobs;

namespace justonlytnt.World;

public sealed partial class VoxelWorld : Node3D
{
	[Export] public WorldConfig Config { get; set; } = new();

	public Node3D? PlayerTarget { get; set; }

	private readonly Dictionary<ChunkCoord, ChunkRuntime> _chunks = new();
	private readonly HashSet<ChunkCoord> _visibleSet = new();
	private readonly Queue<ChunkCoord> _pendingCreateQueue = new();
	private readonly HashSet<ChunkCoord> _pendingCreateSet = new();
	private readonly Queue<ChunkCoord> _pendingRemoveQueue = new();
	private readonly HashSet<ChunkCoord> _pendingRemoveSet = new();
	private readonly Queue<ChunkRenderer> _rendererPool = new();

	private readonly PriorityQueue<IChunkJob, int> _jobQueue = new();
	private readonly object _jobLock = new();
	private readonly AutoResetEvent _jobSignal = new(false);
	private readonly ConcurrentQueue<IChunkJobResult> _resultQueue = new();
	private readonly List<Task> _workers = new();

	private CancellationTokenSource? _workerCts;

	private readonly Queue<BuildChunkMeshResult> _pendingMeshApply = new();
	private double _visibilityTickAccumulator;

	public override void _Ready()
	{
		if (Config is null)
		{
			Config = new WorldConfig();
		}

		_visibilityTickAccumulator = Config.VisibilityRefreshSeconds;
		StartWorkers();
	}

	public override void _ExitTree()
	{
		StopWorkers();
	}

	public override void _Process(double delta)
	{
		DrainResults();
		ApplyMeshResults();
		ProcessChunkStreaming();

		if (PlayerTarget is null)
		{
			return;
		}

		_visibilityTickAccumulator += delta;
		if (_visibilityTickAccumulator >= Config.VisibilityRefreshSeconds)
		{
			_visibilityTickAccumulator = 0.0;
			UpdateVisibleChunks(PlayerTarget.GlobalPosition);
		}

		UpdateChunkVisibility(PlayerTarget.GlobalPosition);
	}

	private sealed class ChunkRuntime
	{
		public ChunkData? Data;
		public int Version;
		public bool GenerationRequested;
		public bool MeshRequested;
		public bool CollisionReady;
		public readonly ChunkRenderer Renderer;

		public ChunkRuntime(ChunkRenderer renderer)
		{
			Renderer = renderer;
		}
	}
}
