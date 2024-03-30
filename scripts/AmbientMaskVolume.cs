using Godot;
using System;
using WildRP.AMVTool.GUI;
using WildRP.AMVTool.Sceneview;

namespace WildRP.AMVTool;

public partial class AmbientMaskVolume : Node3D
{
	[Export] private AmvBoundsMesh _boundsMesh;
	public string ListName { get; private set; }
	public bool Selected => AmvBakerGui.SelectedAmv == this;
	public bool IncludeInFullBake { get; set; } = true;
	public void Setup(string name) => ListName = name;
	public Vector3 Size { get; private set; } = Vector3.One;

	public event Action<AmbientMaskVolume> Deleted;
	public event Action SizeChanged;
	
	public void Delete()
	{
		QueueFree();
		Deleted(this);
	}
}
