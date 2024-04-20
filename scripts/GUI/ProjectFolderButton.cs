using Godot;

namespace WildRP.AMVTool.GUI;

public partial class ProjectFolderButton : Button
{
    public override void _Ready()
    {
        Pressed += () => OS.ShellShowInFileManager(SaveManager.GetProjectPath());
    }
}