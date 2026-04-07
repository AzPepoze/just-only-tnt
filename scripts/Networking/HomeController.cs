using Godot;

namespace justonlytnt.Networking;

public sealed partial class HomeController : Control
{
    private LineEdit? _nameEdit;
    private LineEdit? _ipEdit;
    private SpinBox? _portSpin;
    private LineEdit? _seedEdit;

    public override void _Ready()
    {
        LaunchOptions.ParseCliOnce();
        if (LaunchOptions.Mode == LaunchMode.DedicatedServer)
        {
            GetTree().ChangeSceneToFile("res://scenes/main.tscn");
            return;
        }

        BuildUi();
    }

    private void BuildUi()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        ColorRect bg = new()
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Color = new Color(0.08f, 0.10f, 0.13f, 1f),
        };
        AddChild(bg);

        PanelContainer panel = new()
        {
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -230f,
            OffsetTop = -190f,
            OffsetRight = 230f,
            OffsetBottom = 190f,
        };
        AddChild(panel);

        StyleBoxFlat panelStyle = new()
        {
            BgColor = new Color(0.12f, 0.14f, 0.18f, 0.96f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.25f, 0.29f, 0.35f, 1f),
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        MarginContainer margin = new()
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 14f,
            OffsetTop = 12f,
            OffsetRight = -14f,
            OffsetBottom = -12f,
        };
        panel.AddChild(margin);

        VBoxContainer root = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        Label title = new()
        {
            Text = "JUST ONLY TNT",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        root.AddChild(title);

        _nameEdit = AddTextRow(root, "Name", LaunchOptions.PlayerName);
        _ipEdit = AddTextRow(root, "Server IP", LaunchOptions.JoinAddress);
        _seedEdit = AddTextRow(root, "Seed (optional)", "");

        HBoxContainer portRow = new();
        root.AddChild(portRow);
        Label portLabel = new() { Text = "Port", CustomMinimumSize = new Vector2(120f, 0f), VerticalAlignment = VerticalAlignment.Center };
        portRow.AddChild(portLabel);

        _portSpin = new SpinBox
        {
            MinValue = 1,
            MaxValue = 65535,
            Step = 1,
            Rounded = true,
            Value = LaunchOptions.Port,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        portRow.AddChild(_portSpin);

        HSeparator sep = new();
        root.AddChild(sep);

        Button single = new() { Text = "Single" };
        single.Pressed += StartSingle;
        root.AddChild(single);

        Button host = new() { Text = "Host" };
        host.Pressed += StartHost;
        root.AddChild(host);

        Button join = new() { Text = "Join" };
        join.Pressed += StartJoin;
        root.AddChild(join);

        Label hint = new()
        {
            Text = "Dedicated server: run with --headless --server --port 24567",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hint.AddThemeColorOverride("font_color", new Color(0.78f, 0.82f, 0.88f, 0.9f));
        root.AddChild(hint);
    }

    private static LineEdit AddTextRow(VBoxContainer parent, string labelText, string initial)
    {
        HBoxContainer row = new();
        parent.AddChild(row);

        Label label = new()
        {
            Text = labelText,
            CustomMinimumSize = new Vector2(120f, 0f),
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(label);

        LineEdit edit = new()
        {
            Text = initial,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddChild(edit);
        return edit;
    }

    private void StartSingle()
    {
        LaunchOptions.SetSingle(GetNameValue(), ParseOptionalSeed());

        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    private void StartHost()
    {
        LaunchOptions.SetHost(GetPortValue(), GetNameValue(), ParseOptionalSeed());
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    private void StartJoin()
    {
        string address = _ipEdit?.Text?.Trim() ?? "127.0.0.1";
        LaunchOptions.SetClient(address, GetPortValue(), GetNameValue());
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    private string GetNameValue()
    {
        string value = _nameEdit?.Text?.Trim() ?? "Player";
        return string.IsNullOrEmpty(value) ? "Player" : value;
    }

    private int GetPortValue()
    {
        if (_portSpin is null)
        {
            return LaunchOptions.DefaultPort;
        }

        return Mathf.Clamp((int)_portSpin.Value, 1, 65535);
    }

    private int? ParseOptionalSeed()
    {
        string text = _seedEdit?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return int.TryParse(text, out int parsed) ? parsed : null;
    }
}
