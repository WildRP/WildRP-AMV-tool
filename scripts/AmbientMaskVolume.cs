using Godot;
using System;
using WildRP.AMVTool.Sceneview;

namespace WildRP.AMVTool;

public partial class AmbientMaskVolume : Node3D
{
	[Export] private AmvBounds _bounds;
	public string ListName { get; private set; }
	public bool Selected { get; set; }
	public bool IncludeInFullBake { get; set; } = true;
	public event Action<AmbientMaskVolume> OnDeleted;
	public void Setup(string name) => ListName = name;
	
	public void Delete()
	{
		QueueFree();
		OnDeleted(this);
	}
}
