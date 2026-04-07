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
	public long PendingFillBlocks => _pendingFillBlocks;

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
	private readonly Queue<PendingFillOperation> _pendingFillOperations = new();
	private readonly HashSet<ChunkCoord> _fillDirtyChunks = new();
	private readonly HashSet<ChunkCoord> _fillRemeshChunks = new();
	private long _pendingFillBlocks;
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
		ProcessQueuedFillOperations();
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

	private sealed class PendingFillOperation
	{
		public readonly Vector3I Min;
		public readonly Vector3I Max;
		public readonly BlockType Type;
		public int CurrentX;
		public int CurrentY;
		public int CurrentZ;

		public PendingFillOperation(Vector3I min, Vector3I max, BlockType type)
		{
			Min = min;
			Max = max;
			Type = type;
			CurrentX = min.X;
			CurrentY = min.Y;
			CurrentZ = min.Z;
		}

		public bool IsComplete => CurrentY > Max.Y;

		public Vector3I Current => new(CurrentX, CurrentY, CurrentZ);

		public void Advance()
		{
			CurrentX++;
			if (CurrentX <= Max.X)
			{
				return;
			}

			CurrentX = Min.X;
			CurrentZ++;
			if (CurrentZ <= Max.Z)
			{
				return;
			}

			CurrentZ = Min.Z;
			CurrentY++;
		}
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
