using Godot;
using System;
using WildRP.AMVTool.Autoloads;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool.Sceneview;

public partial class SceneView : Node
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
}
