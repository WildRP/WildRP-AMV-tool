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
        CameraInput(@event);
    }

    private static List<ViewPanel> _sceneViewPanels = [];

    public static void RegisterViewPanel(ViewPanel p) => _sceneViewPanels.Add(p);
}
