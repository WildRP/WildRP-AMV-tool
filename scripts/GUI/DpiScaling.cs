using Godot;
using System;

namespace WildRP.AMVTool.GUI;

public partial class DpiScaling : Control
{
    [Export(PropertyHint.Range, "0.5, 2, ")] public float UiScale = 1.0f;

    public override void _Ready()
    {
        GD.Print(DisplayServer.ScreenGetDpi());
    }

    public override void _Process(double delta)
    {
        float baseDPI = 72;
        float currentDPI = DisplayServer.ScreenGetDpi();
        
        GetWindow().ContentScaleFactor = (currentDPI / baseDPI) * UiScale;
    }
}
