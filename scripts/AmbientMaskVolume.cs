using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;
using WildRP.AMVTool.GUI;
using WildRP.AMVTool.Sceneview;

namespace WildRP.AMVTool;

public partial class AmbientMaskVolume : Node3D
{
	[Export] private AmvBoundsMesh _boundsMesh;
	[Export] private PackedScene _probeScene;
	public string GuiListName { get; private set; }
	public bool Selected => AmvBakerGui.SelectedAmv == this;
	public bool IncludeInFullBake { get; set; } = true;
	public void Setup(string name) => GuiListName = name;

	private Vector3 _size = Vector3.One;

	private readonly List<AmvProbe> _probes = [];

	public Vector3 Size
	{
		get => _size;
		private set => _size = value;
	}
	
	private Vector3 _probeCount = Vector3.One * 2;
	public Vector3 ProbeCount
	{
		get => _probeCount;
		set => _probeCount = value;
	}

	public ulong TextureName { get; set; }

	public event Action<AmbientMaskVolume> Deleted;
	public event Action SizeChanged;

	public override void _Ready()
	{
		GenerateProbes();
		SizeChanged += GenerateProbes;
	}

	// This should really be replaced with a dynamic object pool so we don't delete and recreate probes every time we resize
	public void GenerateProbes()
	{
		_probes.ForEach(p => p.QueueFree());
		_probes.Clear();

		var stepSizeX = Size.X / ProbeCount.X;
		var stepSizeY = Size.Y / ProbeCount.Y;
		var stepSizeZ = Size.Z / ProbeCount.Z;
		
		var probePos = Vector3.Zero;
		var probeCounter = 0;
		
		// a 3-dimensional loop? this is getting silly
		for (int x = 0; x < ProbeCount.X; x++)
		{
			probePos.X = -Size.X/2 + x * stepSizeX + stepSizeX / 2;
			for (int y = 0; y < ProbeCount.Y; y++)
			{
				probePos.Y = -Size.Y / 2 + y * stepSizeY + stepSizeY / 2;
				for (int z = 0; z < ProbeCount.Z; z++)
				{
					probePos.Z = -Size.Z / 2 + z * stepSizeZ + stepSizeZ / 2;

					var finalPos = Basis * probePos;
					var probe = _probeScene.Instantiate() as AmvProbe;
					AddChild(probe);
					_probes.Add(probe);
					
					probe.Position = finalPos;
					probe.GlobalRotation = GlobalRotation;
					probeCounter++;
				}
			}
		}
	}
	
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
	
	public void SetProbesX(double n)
	{
		var v = ProbeCount;
		v.X = (float)n;
		ProbeCount = v;
		SizeChanged();
	}
	
	public void SetProbesZ(double n)
	{
		var v = ProbeCount;
		v.Y = (float)n;
		ProbeCount = v;
		SizeChanged();
	}
	
	public void SetProbesY(double n)
	{
		var v = ProbeCount;
		v.Z = (float)n;
		ProbeCount = v;
		SizeChanged();
	}

	public void SetRotation(double n)
	{
		var v = RotationDegrees;
		v.Y = (float)n;
		RotationDegrees = v;
	}
	#endregion
}
