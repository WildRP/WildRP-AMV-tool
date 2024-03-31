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

	private Vector3 _size = Vector3.One;

	public Vector3 Size
	{
		get => _size;
		set
		{
			SizeChanged();
			_size = value;
		} 
		
	}
	
	private Vector3 _spacing = Vector3.One;
	public Vector3 Spacing
	{
		get;
		set;
	}

	public ulong TextureName { get; set; }

	public event Action<AmbientMaskVolume> Deleted;
	public event Action SizeChanged;
	
	public void Delete()
	{
		QueueFree();
		Deleted(this);
	}

	public void ChangeSizeWithGizmo(Vector3 diff, bool positive)
	{
		
		_size += diff;
		
		if (positive)
			diff *= -1;
		
		Position -= Basis * (diff / 2);
		
		SizeChanged();
	}
}
