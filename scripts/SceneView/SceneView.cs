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

    private int _debugRenderPass;
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey {Pressed: true} key)
        {
            _debugRenderPass = key.Keycode switch
            {
                Key.Key1 => 0,
                Key.Key2 => 1,
                Key.Key3 => 2,
                Key.Key4 => 3,
                Key.Key5 => 4,
                Key.Key6 => 5,
                Key.Key7 => 6,
                _ => _debugRenderPass
            };
            RenderingServer.GlobalShaderParameterSet("probe_rendering_pass", _debugRenderPass);

            if (key.Keycode == Key.C && OS.HasFeature("editor"))
                DeferredProbeBaker.Instance.EnableDebugProbeView();
        }
        
        CameraInput(@event);
    }

    private static List<ViewPanel> _sceneViewPanels = [];

    public static void RegisterViewPanel(ViewPanel p) => _sceneViewPanels.Add(p);
}
