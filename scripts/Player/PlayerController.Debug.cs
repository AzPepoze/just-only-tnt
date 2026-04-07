using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Godot;
using justonlytnt.World;

namespace justonlytnt.Player;

public sealed partial class PlayerController
{
    private CanvasLayer? _debugHudLayer;
    private RichTextLabel? _debugHudLabel;
    private bool _debugHudVisible;
    private double _debugHudRefreshTimer;

    private string _cpuName = "Unknown";
    private int _cpuLogicalCoreCount = System.Environment.ProcessorCount;
    private string _gpuName = "Unknown";
    private string _gpuVendor = "Unknown";

    private double _lastCpuSampleWallSeconds;
    private double _lastCpuSampleProcessSeconds;
    private float _processCpuUsagePercent;

    private void BuildDebugOverlay()
    {
        _debugHudLayer = new CanvasLayer();
        AddChild(_debugHudLayer);

        PanelContainer panel = new()
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 0f,
            AnchorBottom = 0f,
            OffsetLeft = 10f,
            OffsetTop = 10f,
            OffsetRight = 590f,
            OffsetBottom = 420f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _debugHudLayer.AddChild(panel);

        StyleBoxFlat panelStyle = new()
        {
            BgColor = new Color(0f, 0f, 0f, 0.72f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        _debugHudLabel = new RichTextLabel
        {
            BbcodeEnabled = false,
            ScrollActive = false,
            SelectionEnabled = false,
            FitContent = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(560f, 380f),
        };
        _debugHudLabel.AddThemeColorOverride("default_color", new Color(0.95f, 0.95f, 0.95f, 1f));
        panel.AddChild(_debugHudLabel);

        _debugHudVisible = false;
        _debugHudLayer.Visible = false;
    }

    private void CacheHardwareInfo()
    {
        _cpuLogicalCoreCount = System.Environment.ProcessorCount;
        _cpuName = TryCallStaticMethod(typeof(OS), "GetProcessorName") ?? $"Unknown CPU ({_cpuLogicalCoreCount} logical cores)";

        _gpuName =
            TryCallStaticMethod(typeof(RenderingServer), "GetVideoAdapterName") ??
            TryCallStaticMethod(typeof(DisplayServer), "GetVideoAdapterName") ??
            "Unknown GPU";

        _gpuVendor =
            TryCallStaticMethod(typeof(RenderingServer), "GetVideoAdapterVendor") ??
            TryCallStaticMethod(typeof(DisplayServer), "GetVideoAdapterVendor") ??
            "Unknown Vendor";

        _lastCpuSampleWallSeconds = Time.GetTicksUsec() / 1_000_000.0;
        _lastCpuSampleProcessSeconds = Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds;
    }

    private void ToggleDebugOverlay()
    {
        _debugHudVisible = !_debugHudVisible;
        if (_debugHudLayer is not null)
        {
            _debugHudLayer.Visible = _debugHudVisible;
        }

        _debugHudRefreshTimer = 0.2;
    }

    private void UpdateDebugOverlay(double delta)
    {
        if (!_debugHudVisible || _debugHudLabel is null)
        {
            return;
        }

        _debugHudRefreshTimer += delta;
        if (_debugHudRefreshTimer < 0.12)
        {
            return;
        }

        _debugHudRefreshTimer = 0.0;
        RefreshProcessCpuUsage();

        double fps = Engine.GetFramesPerSecond();
        float frameMs = fps > 0.0 ? (float)(1000.0 / fps) : 0f;
        Vector3 position = GlobalPosition;
        Vector3 velocity = Velocity;
        ChunkCoord? chunk = null;

        if (_world is not null)
        {
            chunk = _world.WorldToChunk(ToBlock(position));
        }

        double processSec = GetMonitorOrDefault("TimeProcess");
        double physicsSec = GetMonitorOrDefault("TimePhysicsProcess");
        double navigationSec = GetFirstMonitorOrDefault("TimeNavigationProcess", "NavigationProcess");
        double gpuSec = GetFirstMonitorOrDefault("TimeGpu", "RenderTimeGpu");

        float cpuFrameMs = (float)((processSec + physicsSec + navigationSec) * 1000.0);
        float cpuMainThreadLoad = frameMs > 0.0001f ? (cpuFrameMs / frameMs) * 100f : 0f;
        float gpuFrameMs = (float)(gpuSec * 1000.0);
        float gpuFrameLoad = frameMs > 0.0001f ? (gpuFrameMs / frameMs) * 100f : 0f;

        double drawCalls = GetFirstMonitorOrDefault("RenderTotalDrawCallsInFrame", "RenderingTotalDrawCallsInFrame");
        double objects = GetFirstMonitorOrDefault("RenderTotalObjectsInFrame", "RenderingTotalObjectsInFrame");
        double primitives = GetFirstMonitorOrDefault("RenderTotalPrimitivesInFrame", "RenderingTotalPrimitivesInFrame");

        double staticMemory = GetMonitorOrDefault("MemoryStatic");
        double dynamicMemory = GetMonitorOrDefault("MemoryDynamic");
        double videoMemory = GetFirstMonitorOrDefault("RenderingVideoMemUsed", "RenderVideoMemUsed");

        StringBuilder sb = new(768);
        sb.AppendLine("F3 DEBUG");
        sb.AppendLine($"FPS: {fps} ({frameMs:0.00} ms/frame)");
        sb.AppendLine($"Position: X {position.X:0.00} | Y {position.Y:0.00} | Z {position.Z:0.00}");
        sb.AppendLine($"Velocity: X {velocity.X:0.00} | Y {velocity.Y:0.00} | Z {velocity.Z:0.00}");
        sb.AppendLine($"Mode: {(_isFlying ? "Flying" : "Grounded")}  |  Sprinting: {_cachedSprintHeld}");
        sb.AppendLine(chunk.HasValue
            ? $"Chunk: X {chunk.Value.X} | Z {chunk.Value.Z}"
            : "Chunk: N/A");

        sb.AppendLine();
        sb.AppendLine("CPU");
        sb.AppendLine($"Name: {_cpuName}");
        sb.AppendLine($"Logical Cores: {_cpuLogicalCoreCount}");
        sb.AppendLine($"Process Usage: {_processCpuUsagePercent:0.0}%");
        sb.AppendLine($"Main Thread Frame Time: {cpuFrameMs:0.00} ms");
        sb.AppendLine($"Main Thread Load Estimate: {cpuMainThreadLoad:0.0}%");
        sb.AppendLine($"  - Process Step: {processSec * 1000.0:0.00} ms");
        sb.AppendLine($"  - Physics Step: {physicsSec * 1000.0:0.00} ms");
        sb.AppendLine($"  - Navigation Step: {navigationSec * 1000.0:0.00} ms");

        sb.AppendLine();
        sb.AppendLine("GPU");
        sb.AppendLine($"Name: {_gpuName}");
        sb.AppendLine($"Vendor: {_gpuVendor}");
        if (gpuSec > 0.0000001)
        {
            sb.AppendLine($"Frame Time (GPU monitor): {gpuFrameMs:0.00} ms");
            sb.AppendLine($"Frame Load Estimate: {gpuFrameLoad:0.0}%");
        }
        else
        {
            sb.AppendLine("Usage: Not exposed by this renderer/driver monitor");
        }
        sb.AppendLine($"Draw Calls: {drawCalls:0}");
        sb.AppendLine($"Objects: {objects:0}");
        sb.AppendLine($"Primitives: {primitives:0}");

        sb.AppendLine();
        sb.AppendLine("Memory");
        sb.AppendLine($"Static: {FormatBytes(staticMemory)}");
        sb.AppendLine($"Dynamic: {FormatBytes(dynamicMemory)}");
        sb.AppendLine($"Video (VRAM): {FormatBytes(videoMemory)}");

        _debugHudLabel.Text = sb.ToString();
    }

    private void RefreshProcessCpuUsage()
    {
        double nowWallSeconds = Time.GetTicksUsec() / 1_000_000.0;
        double nowProcessSeconds = Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds;

        double wallDelta = nowWallSeconds - _lastCpuSampleWallSeconds;
        double processDelta = nowProcessSeconds - _lastCpuSampleProcessSeconds;
        if (wallDelta > 0.00001 && _cpuLogicalCoreCount > 0)
        {
            double ratio = processDelta / (wallDelta * _cpuLogicalCoreCount);
            _processCpuUsagePercent = (float)Math.Clamp(ratio * 100.0, 0.0, 1500.0);
        }

        _lastCpuSampleWallSeconds = nowWallSeconds;
        _lastCpuSampleProcessSeconds = nowProcessSeconds;
    }

    private static string? TryCallStaticMethod(Type type, string methodName)
    {
        MethodInfo? method = type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (method is null)
        {
            return null;
        }

        try
        {
            object? result = method.Invoke(null, null);
            return result?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static double GetMonitorOrDefault(string monitorName)
    {
        return TryGetMonitor(monitorName, out double value) ? value : 0.0;
    }

    private static double GetFirstMonitorOrDefault(params string[] monitorNames)
    {
        for (int i = 0; i < monitorNames.Length; i++)
        {
            if (TryGetMonitor(monitorNames[i], out double value))
            {
                return value;
            }
        }

        return 0.0;
    }

    private static bool TryGetMonitor(string monitorName, out double value)
    {
        value = 0.0;
        if (!Enum.TryParse(monitorName, ignoreCase: false, out Performance.Monitor monitor))
        {
            return false;
        }

        Variant monitorValue = Performance.GetMonitor(monitor);
        string numeric = monitorValue.ToString();
        return double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatBytes(double bytes)
    {
        if (bytes <= 0)
        {
            return "N/A";
        }

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int unitIndex = 0;
        double value = bytes;
        while (value >= 1024.0 && unitIndex < units.Length - 1)
        {
            value /= 1024.0;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
