using Godot;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool.Sceneview;

public partial class AmvBoundsMesh : BoundsMesh
{
    public override void _Ready()
    {
        base._Ready();
        AmvBakerGui.GuiToggled += SetVisible;
    }

    public override void _ExitTree()
    {
        AmvBakerGui.GuiToggled -= SetVisible;
    }
}