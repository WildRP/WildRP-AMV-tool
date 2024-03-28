using Godot;
using System;
using WildRP.AMVTool.Autoloads;

namespace WildRP.AMVTool.GUI;

public partial class DpiScaling : Control
{

    public override void _Ready()
    {
        GD.Print(DisplayServer.ScreenGetDpi());
    }

    public override void _Process(double delta)
    {
        
        
        GetWindow().ContentScaleFactor = GetScale();
    }

    public static float GetScale()
    {
        float baseDPI = 72;
        float currentDPI = DisplayServer.ScreenGetDpi();

        return (currentDPI / baseDPI) * Settings.UiScale;
    }
}
