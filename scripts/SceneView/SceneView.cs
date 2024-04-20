using System.Collections.Generic;
using Godot;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool.Sceneview;

public partial class SceneView : Node3D
{
    public override void _Ready()
    {
        SetupCamera();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;

        ProcessCamera(dt);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey {Pressed: true} key)
        {
            int pass = 0;
            if (key.Keycode == Key.Key1)
                pass = 0;
            if (key.Keycode == Key.Key2)
                pass = 1;
            if (key.Keycode == Key.Key3)
                pass = 2;
            if (key.Keycode == Key.Key4)
                pass = 3;
            if (key.Keycode == Key.Key5)
                pass = 4;
            if (key.Keycode == Key.Key6)
                pass = 5;
            if (key.Keycode == Key.Key7)
                pass = 6;

            RenderingServer.GlobalShaderParameterSet("probe_rendering_pass", pass);
        }
        
        CameraInput(@event);
    }

    private static List<ViewPanel> _sceneViewPanels = [];

    public static void RegisterViewPanel(ViewPanel p) => _sceneViewPanels.Add(p);
}
