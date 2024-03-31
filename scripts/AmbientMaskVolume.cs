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
	
	// These are used to connect to the UI
	#region UIConnectFunctions

	public void SetSizeX(double n)
	{
		var v = Size;
		v.X = (float)n;
		Size = v;
		SizeChanged();
	}
	
	public void SetSizeY(double n)
	{
		var v = Size;
		v.Y = (float)n;
		Size = v;
		SizeChanged();
	}
	
	public void SetSizeZ(double n)
	{
		var v = Size;
		v.Z = (float)n;
		Size = v;
		SizeChanged();
	}
	
	public void SetPositionX(double n)
	{
		var v = Position;
		v.X = (float)n;
		Position = v;
		SizeChanged();
	}
	
	public void SetPositionY(double n)
	{
		var v = Position;
		v.Y = (float)n;
		Position = v;
		SizeChanged();
	}
	
	public void SetPositionZ(double n)
	{
		var v = Position;
		v.Z = (float)n;
		Position = v;
		SizeChanged();
	}
	
	public void SetSpacingX(double n)
	{
		var v = Spacing;
		v.X = (float)n;
		Spacing = v;
		SizeChanged();
	}
	
	public void SetSpacingY(double n)
	{
		var v = Spacing;
		v.Y = (float)n;
		Spacing = v;
		SizeChanged();
	}
	
	public void SetSpacingZ(double n)
	{
		var v = Spacing;
		v.Z = (float)n;
		Spacing = v;
		SizeChanged();
	}
	#endregion
}
