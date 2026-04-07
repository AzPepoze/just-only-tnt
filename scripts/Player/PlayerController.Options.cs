using Godot;
using justonlytnt;
using justonlytnt.World;

namespace justonlytnt.Player;

public sealed partial class PlayerController
{
    private void BuildOptionsMenu()
    {
        _optionsLayer = new CanvasLayer
        {
            Visible = false,
        };
        AddChild(_optionsLayer);

        ColorRect backdrop = new()
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Color = new Color(0f, 0f, 0f, 0.58f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _optionsLayer.AddChild(backdrop);

        PanelContainer panel = new()
        {
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -360f,
            OffsetTop = -280f,
            OffsetRight = 360f,
            OffsetBottom = 280f,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        backdrop.AddChild(panel);

        StyleBoxFlat panelStyle = new()
        {
            BgColor = new Color(0.10f, 0.10f, 0.10f, 0.96f),
            BorderColor = new Color(0.35f, 0.35f, 0.35f, 1f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
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

        VBoxContainer layout = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        Label title = new()
        {
            Text = "Options",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        layout.AddChild(title);

        Label hint = new()
        {
            Text = "ESC closes this menu. Terrain generation settings apply fully after generating a new world.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        hint.AddThemeColorOverride("font_color", new Color(0.84f, 0.84f, 0.84f, 1f));
        layout.AddChild(hint);

        ScrollContainer scroll = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0f, 390f),
        };
        layout.AddChild(scroll);

        VBoxContainer content = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        content.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(content);

        WorldConfig config = _world?.Config ?? new WorldConfig();

        AddSectionHeader(content, "Player");
        AddFloatSettingRow(content, "Walk Speed", 1.0, 30.0, 0.1, WalkSpeed, value => WalkSpeed = (float)value);
        AddFloatSettingRow(content, "Sprint Speed", 1.0, 40.0, 0.1, SprintSpeed, value => SprintSpeed = (float)value);
        AddFloatSettingRow(content, "Jump Velocity", 1.0, 25.0, 0.1, JumpVelocity, value => JumpVelocity = (float)value);
        AddFloatSettingRow(content, "Mouse Sensitivity", 0.0005, 0.02, 0.0001, MouseSensitivity, value => MouseSensitivity = (float)value);
        AddFloatSettingRow(content, "Fly Speed", 1.0, 40.0, 0.1, FlySpeed, value => FlySpeed = (float)value);
        AddFloatSettingRow(content, "Fly Sprint Speed", 1.0, 60.0, 0.1, FlySprintSpeed, value => FlySprintSpeed = (float)value);
        AddFloatSettingRow(content, "Fill Hold Seconds", 0.1, 1.2, 0.05, FillHoldSeconds, value => FillHoldSeconds = (float)value);

        AddSectionHeader(content, "World");
        AddIntSettingRow(content, "Seed", -2000000000, 2000000000, config.Seed, value =>
        {
            if (_world is not null)
            {
                _world.Config.Seed = value;
            }
        });
        AddIntSettingRow(content, "View Distance Chunks", 2, 24, config.ViewDistanceChunks, value =>
        {
            if (_world is not null)
            {
                _world.Config.ViewDistanceChunks = value;
            }
        });
        AddIntSettingRow(content, "Base Terrain Height", 8, 128, config.BaseTerrainHeight, value =>
        {
            if (_world is not null)
            {
                _world.Config.BaseTerrainHeight = value;
            }
        });
        AddIntSettingRow(content, "Terrain Amplitude", 4, 64, config.TerrainAmplitude, value =>
        {
            if (_world is not null)
            {
                _world.Config.TerrainAmplitude = value;
            }
        });
        AddFloatSettingRow(content, "Terrain Frequency", 0.001, 0.2, 0.001, config.TerrainFrequency, value =>
        {
            if (_world is not null)
            {
                _world.Config.TerrainFrequency = (float)value;
            }
        });
        AddFloatSettingRow(content, "Interaction Distance", 1.0, 100.0, 1.0, config.InteractionDistance, value =>
        {
            if (_world is not null)
            {
                _world.Config.InteractionDistance = (float)value;
            }
        });

        AddSectionHeader(content, "Performance");
        AddIntSettingRow(content, "Fill Blocks / Frame", 64, 200000, config.FillBlocksPerFrame, value =>
        {
            if (_world is not null)
            {
                _world.Config.FillBlocksPerFrame = value;
            }
        });
        AddIntSettingRow(content, "Max Explosions / Frame", 1, 64, config.MaxExplosionsPerFrame, value =>
        {
            if (_world is not null)
            {
                _world.Config.MaxExplosionsPerFrame = value;
            }
        });
        AddIntSettingRow(content, "Max Chain Spawns / Frame", 1, 512, config.MaxChainSpawnsPerFrame, value =>
        {
            if (_world is not null)
            {
                _world.Config.MaxChainSpawnsPerFrame = value;
            }
        });
        AddIntSettingRow(content, "Max Active Primed TNT", 16, 4096, config.MaxActivePrimedTnt, value =>
        {
            if (_world is not null)
            {
                _world.Config.MaxActivePrimedTnt = value;
            }
        });
        AddIntSettingRow(content, "Max Debris Spawns / Frame", 1, 512, config.MaxDebrisSpawnsPerFrame, value =>
        {
            if (_world is not null)
            {
                _world.Config.MaxDebrisSpawnsPerFrame = value;
            }
        });

        AddSectionHeader(content, "TNT");
        AddFloatSettingRow(content, "Fuse Seconds", 0.1, 10.0, 0.1, config.TntFuseSeconds, value =>
        {
            if (_world is not null)
            {
                _world.Config.TntFuseSeconds = (float)value;
            }
        });
        AddFloatSettingRow(content, "Blast Radius", 1.0, 16.0, 0.5, config.TntBlastRadius, value =>
        {
            if (_world is not null)
            {
                _world.Config.TntBlastRadius = (float)value;
            }
        });
        AddFloatSettingRow(content, "Explosion Impulse", 0.1, 200.0, 0.1, config.TntExplosionImpulse, value =>
        {
            if (_world is not null)
            {
                _world.Config.TntExplosionImpulse = (float)value;
            }
        });
        AddIntSettingRow(content, "Debris Max / Explosion", 0, 512, config.DebrisMaxPerExplosion, value =>
        {
            if (_world is not null)
            {
                _world.Config.DebrisMaxPerExplosion = value;
            }
        });
        AddFloatSettingRow(content, "Debris Lifetime", 0.1, 10.0, 0.1, config.DebrisLifetimeSeconds, value =>
        {
            if (_world is not null)
            {
                _world.Config.DebrisLifetimeSeconds = (float)value;
            }
        });
        AddFloatSettingRow(content, "Debris Impulse", 0.1, 100.0, 0.1, config.DebrisImpulse, value =>
        {
            if (_world is not null)
            {
                _world.Config.DebrisImpulse = (float)value;
            }
        });

        AddSectionHeader(content, "Effects");
        AddToggleRow(content, "Spawn Mini Debris On Explosion", config.SpawnDebrisEnabled, enabled =>
        {
            if (_world is not null)
            {
                _world.Config.SpawnDebrisEnabled = enabled;
            }
        });

        HSeparator separator = new();
        content.AddChild(separator);

        HBoxContainer actions = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        actions.AddThemeConstantOverride("separation", 10);
        content.AddChild(actions);

        Button generateButton = new()
        {
            Text = "Generate New World",
            CustomMinimumSize = new Vector2(210f, 38f),
        };
        generateButton.Pressed += () => GenerateNewWorld(false);
        actions.AddChild(generateButton);

        Button randomGenerateButton = new()
        {
            Text = "Random Seed + Generate",
            CustomMinimumSize = new Vector2(210f, 38f),
        };
        randomGenerateButton.Pressed += () => GenerateNewWorld(true);
        actions.AddChild(randomGenerateButton);

        Button closeButton = new()
        {
            Text = "Close",
            CustomMinimumSize = new Vector2(110f, 38f),
        };
        closeButton.Pressed += () => SetOptionsMenuOpen(false);
        actions.AddChild(closeButton);
    }

    private static void AddSectionHeader(VBoxContainer parent, string text)
    {
        HSeparator separator = new();
        parent.AddChild(separator);

        Label label = new()
        {
            Text = text,
        };
        label.AddThemeColorOverride("font_color", new Color(0.98f, 0.90f, 0.40f, 1f));
        label.AddThemeFontSizeOverride("font_size", 15);
        parent.AddChild(label);
    }

    private static void AddIntSettingRow(VBoxContainer parent, string labelText, int min, int max, int initial, System.Action<int> onChanged)
    {
        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        parent.AddChild(row);

        Label label = new()
        {
            Text = labelText,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(label);

        SpinBox spin = new()
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Rounded = true,
            Value = initial,
            CustomMinimumSize = new Vector2(180f, 0f),
        };
        spin.ValueChanged += value => onChanged((int)System.Math.Round(value));
        row.AddChild(spin);
    }

    private static void AddFloatSettingRow(VBoxContainer parent, string labelText, double min, double max, double step, double initial, System.Action<double> onChanged)
    {
        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        parent.AddChild(row);

        Label label = new()
        {
            Text = labelText,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(label);

        SpinBox spin = new()
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = initial,
            CustomMinimumSize = new Vector2(180f, 0f),
        };
        spin.ValueChanged += value => onChanged(value);
        row.AddChild(spin);
    }

    private static void AddToggleRow(VBoxContainer parent, string text, bool initial, System.Action<bool> onChanged)
    {
        CheckBox toggle = new()
        {
            Text = text,
            ButtonPressed = initial,
        };
        toggle.Toggled += value => onChanged(value);
        parent.AddChild(toggle);
    }

    private void ToggleOptionsMenu()
    {
        SetOptionsMenuOpen(!_optionsMenuOpen);
    }

    private void SetOptionsMenuOpen(bool open)
    {
        _optionsMenuOpen = open;

        if (_optionsLayer is not null)
        {
            _optionsLayer.Visible = open;
        }

        if (open)
        {
            ResetFillPlacementState();
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void GenerateNewWorld(bool randomizeSeed)
    {
        if (_world is null)
        {
            return;
        }

        if (Multiplayer.MultiplayerPeer is not null && !Multiplayer.IsServer())
        {
            return;
        }

        if (randomizeSeed)
        {
            RandomNumberGenerator rng = new();
            rng.Randomize();
            _world.Config.Seed = rng.RandiRange(-2000000000, 2000000000);
        }

        GameBootstrap.QueueNextWorld(_world.Config, CapturePlayerSettings());
        GetTree().ReloadCurrentScene();
    }

    private GameBootstrap.PlayerRuntimeSettings CapturePlayerSettings()
    {
        return new GameBootstrap.PlayerRuntimeSettings(
            WalkSpeed,
            SprintSpeed,
            JumpVelocity,
            MouseSensitivity,
            FlySpeed,
            FlySprintSpeed,
            FillHoldSeconds);
    }
}
