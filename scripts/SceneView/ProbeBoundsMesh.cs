using Godot;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool.Sceneview;

public partial class ProbeBoundsMesh : BoundsMesh
{
    public override void _Ready()
    {
        base._Ready();
        DeferredProbesUi.GuiToggled += SetVisible;
    }

    public override void _ExitTree()
    {
        DeferredProbesUi.GuiToggled -= SetVisible;
    }
}