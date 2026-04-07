using Godot;

namespace justonlytnt.Player;

public sealed partial class PlayerController
{
    public override void _Process(double delta)
    {
        if (_optionsMenuOpen)
        {
            _cachedMoveInput = Vector2.Zero;
            _cachedJumpHeld = false;
            _cachedDescendHeld = false;
            _cachedSprintHeld = false;
            _cachedPlaceHeld = false;
            _jumpPressedQueued = false;
            _placePressedQueued = false;
            _placeReleasedQueued = false;
            _ignitePressedQueued = false;
            UpdateDebugOverlay(delta);
            return;
        }

        _cachedMoveInput = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        _cachedJumpHeld = Input.IsActionPressed("jump");
        _cachedDescendHeld = Input.IsActionPressed("descend");
        _cachedSprintHeld = Input.IsActionPressed("sprint");
        _cachedPlaceHeld = Input.IsActionPressed("place_tnt");

        if (Input.IsActionJustPressed("jump"))
        {
            _jumpPressedQueued = true;
        }

        if (Input.IsActionJustPressed("place_tnt"))
        {
            _placePressedQueued = true;
        }

        if (Input.IsActionJustReleased("place_tnt"))
        {
            _placeReleasedQueued = true;
        }

        if (Input.IsActionJustPressed("ignite_tnt"))
        {
            _ignitePressedQueued = true;
        }

        UpdateDebugOverlay(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey esc && esc.Pressed && !esc.Echo && esc.Keycode == Key.Escape)
        {
            ToggleOptionsMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_optionsMenuOpen)
        {
            return;
        }

        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * MouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - (motion.Relative.Y * MouseSensitivity), -1.45f, 1.45f);
            if (_camera is not null)
            {
                _camera.Rotation = new Vector3(_pitch, 0f, 0f);
            }
        }

        if (@event is InputEventKey f3 && f3.Pressed && !f3.Echo && f3.Keycode == Key.F3)
        {
            ToggleDebugOverlay();
        }

        if (@event is InputEventKey numberKey && numberKey.Pressed && !numberKey.Echo)
        {
            if (numberKey.Keycode == Key.Key1)
            {
                SetSelectedItem(HotbarItem.Tnt);
            }
            else if (numberKey.Keycode == Key.Key2)
            {
                SetSelectedItem(HotbarItem.FlintAndSteel);
            }
        }

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                CycleSelectedItem(-1);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                CycleSelectedItem(1);
            }
        }
    }

    private static void EnsureInputActions()
    {
        EnsureAction("move_forward", Key.W);
        EnsureAction("move_back", Key.S);
        EnsureAction("move_left", Key.A);
        EnsureAction("move_right", Key.D);
        BindActionToSingleKey("jump", Key.Space);
        BindActionToSingleKey("descend", Key.Shift);
        BindActionToSingleKey("sprint", Key.Ctrl);

        if (!InputMap.HasAction("place_tnt"))
        {
            InputMap.AddAction("place_tnt");
            InputEventMouseButton rightClick = new() { ButtonIndex = MouseButton.Right };
            InputMap.ActionAddEvent("place_tnt", rightClick);
        }

        if (!InputMap.HasAction("ignite_tnt"))
        {
            InputMap.AddAction("ignite_tnt");
            InputEventMouseButton leftClick = new() { ButtonIndex = MouseButton.Left };
            InputMap.ActionAddEvent("ignite_tnt", leftClick);
        }
    }

    private static void EnsureAction(string action, Key key)
    {
        if (InputMap.HasAction(action))
        {
            return;
        }

        InputMap.AddAction(action);
        InputEventKey ev = new() { Keycode = key };
        InputMap.ActionAddEvent(action, ev);
    }

    private static void BindActionToSingleKey(string action, Key key)
    {
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        Godot.Collections.Array<InputEvent> events = InputMap.ActionGetEvents(action);
        for (int i = 0; i < events.Count; i++)
        {
            InputMap.ActionEraseEvent(action, events[i]);
        }

        InputEventKey ev = new() { Keycode = key };
        InputMap.ActionAddEvent(action, ev);
    }
}
