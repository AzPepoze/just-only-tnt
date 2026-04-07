using Godot;

namespace justonlytnt.Player;

public sealed partial class PlayerController
{
    private void CycleSelectedItem(int delta)
    {
        int itemCount = 2;
        int current = (int)_selectedItem;
        int next = (current + delta + itemCount) % itemCount;
        _selectedItem = (HotbarItem)next;
        UpdateHotbarUi();
    }

    private void BuildHotbarUi()
    {
        CanvasLayer hudLayer = new();
        AddChild(hudLayer);

        Control root = new()
        {
            AnchorLeft = 0.5f,
            AnchorTop = 1.0f,
            AnchorRight = 0.5f,
            AnchorBottom = 1.0f,
            OffsetLeft = -120f,
            OffsetTop = -68f,
            OffsetRight = 120f,
            OffsetBottom = -8f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hudLayer.AddChild(root);

        HBoxContainer hotbar = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        hotbar.AddThemeConstantOverride("separation", 10);
        root.AddChild(hotbar);

        _slot1Panel = CreateHotbarSlot("1 TNT", out _slot1Label);
        _slot2Panel = CreateHotbarSlot("2 Flint", out _slot2Label);
        hotbar.AddChild(_slot1Panel);
        hotbar.AddChild(_slot2Panel);
    }

    private static PanelContainer CreateHotbarSlot(string text, out Label label)
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(108f, 44f),
        };

        label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        panel.AddChild(label);
        return panel;
    }

    private void UpdateHotbarUi()
    {
        if (_slot1Panel is null || _slot2Panel is null || _slot1Label is null || _slot2Label is null)
        {
            return;
        }

        bool isTntSelected = _selectedItem == HotbarItem.Tnt;
        Color selectedBg = new(0.95f, 0.88f, 0.30f, 0.95f);
        Color normalBg = new(0.12f, 0.12f, 0.12f, 0.82f);
        Color selectedText = new(0.05f, 0.05f, 0.05f, 1f);
        Color normalText = new(0.95f, 0.95f, 0.95f, 1f);

        StyleBoxFlat slot1Style = new()
        {
            BgColor = isTntSelected ? selectedBg : normalBg,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };

        StyleBoxFlat slot2Style = new()
        {
            BgColor = !isTntSelected ? selectedBg : normalBg,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        };

        _slot1Panel.AddThemeStyleboxOverride("panel", slot1Style);
        _slot2Panel.AddThemeStyleboxOverride("panel", slot2Style);
        _slot1Label.AddThemeColorOverride("font_color", isTntSelected ? selectedText : normalText);
        _slot2Label.AddThemeColorOverride("font_color", !isTntSelected ? selectedText : normalText);
    }
}
