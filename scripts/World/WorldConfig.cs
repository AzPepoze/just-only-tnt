using Godot;

namespace justonlytnt.World;

[GlobalClass]
public partial class WorldConfig : Resource
{
	[Export] public int Seed { get; set; } = 1337;

	[Export(PropertyHint.Range, "8,64,1")] public int ChunkSize { get; set; } = 16;
	[Export(PropertyHint.Range, "32,256,1")] public int ChunkHeight { get; set; } = 96;
	[Export(PropertyHint.Range, "2,24,1")] public int ViewDistanceChunks { get; set; } = 10;

	[Export(PropertyHint.Range, "1,16,1")] public int WorkerCount { get; set; } = 4;
	[Export(PropertyHint.Range, "1,64,1")] public int MaxMainThreadAppliesPerFrame { get; set; } = 6;
	[Export(PropertyHint.Range, "1,8,1")] public int MaxCollisionBuildsPerFrame { get; set; } = 1;
	[Export(PropertyHint.Range, "1,16,1")] public int MaxChunkCreatesPerFrame { get; set; } = 2;
	[Export(PropertyHint.Range, "1,32,1")] public int MaxChunkRemovalsPerFrame { get; set; } = 4;
	[Export(PropertyHint.Range, "0.01,0.5,0.01")] public float VisibilityRefreshSeconds { get; set; } = 0.08f;

	[Export(PropertyHint.Range, "8,128,1")] public int BaseTerrainHeight { get; set; } = 28;
	[Export(PropertyHint.Range, "4,64,1")] public int TerrainAmplitude { get; set; } = 18;
	[Export(PropertyHint.Range, "0.001,0.2,0.001")] public float TerrainFrequency { get; set; } = 0.04f;
	[Export(PropertyHint.Range, "64,200000,1")] public int FillBlocksPerFrame { get; set; } = 12000;
	[Export(PropertyHint.Range, "1,64,1")] public int MaxExplosionsPerFrame { get; set; } = 4;
	[Export(PropertyHint.Range, "1,512,1")] public int MaxChainSpawnsPerFrame { get; set; } = 24;
	[Export(PropertyHint.Range, "16,4096,1")] public int MaxActivePrimedTnt { get; set; } = 512;
	[Export(PropertyHint.Range, "1,512,1")] public int MaxDebrisSpawnsPerFrame { get; set; } = 96;

	[Export(PropertyHint.Range, "0.1,10.0,0.1")] public float TntFuseSeconds { get; set; } = 2.0f;
	[Export(PropertyHint.Range, "1.0,16.0,0.5")] public float TntBlastRadius { get; set; } = 4.5f;
	[Export(PropertyHint.Range, "0.1,200.0,0.1")] public float TntExplosionImpulse { get; set; } = 26.0f;
	[Export(PropertyHint.Range, "0.05,2.0,0.05")] public float TntChainFuseMinSeconds { get; set; } = 0.15f;
	[Export(PropertyHint.Range, "0.1,5.0,0.05")] public float TntChainFuseMaxSeconds { get; set; } = 0.8f;
	[Export(PropertyHint.Range, "0,512,1")] public int DebrisMaxPerExplosion { get; set; } = 64;
	[Export(PropertyHint.Range, "0.1,10.0,0.1")] public float DebrisLifetimeSeconds { get; set; } = 2.2f;
	[Export(PropertyHint.Range, "0.1,100.0,0.1")] public float DebrisImpulse { get; set; } = 14.0f;
	[Export] public bool SpawnDebrisEnabled { get; set; } = true;
	[Export(PropertyHint.Range, "1.0,100.0,1.0")] public float InteractionDistance { get; set; } = 9.0f;
}
