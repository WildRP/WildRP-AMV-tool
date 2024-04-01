using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;
using WildRP.AMVTool.Autoloads;
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

	private bool _baked;
	public bool Baked
	{
		get;
		set;
	}

	private int _samples;

	public int Samples
	{
		get => _samples;
		set
		{
			_samples = value;
			if (_samples >= AmvBaker.GetSampleCount()) Baked = true;
		}
	}

	private Vector3 _size = Vector3.One;

	private readonly List<AmvProbe> _probes = [];

	public Vector3 Size
	{
		get => _size;
		private set => _size = value;
	}
	
	private Vector3I _probeCount = Vector3I.One * 2;
	public Vector3I ProbeCount
	{
		get => _probeCount;
		set => _probeCount = value;
	}

	public ulong TextureName { get; set; }

	public event Action<AmbientMaskVolume> Deleted;
	public event Action SizeChanged;

	public void CaptureSample()
	{
		foreach (var probe in _probes)
		{
			probe.CaptureSample();
		}

		Samples++;
	}

	public void FinalizeBake()
	{
		foreach (var probe in _probes)
		{
			probe.UpdateAverage();
		}
	}
	
	public void ClearSamples()
	{
		Samples = 0;
		Baked = false;
		foreach (var probe in _probes)
		{
			probe.Reset();
		}
	}
	
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
		
		
		// a 3-dimensional loop!
		for (int x = 0; x < ProbeCount.X; x++)
		{
			probePos.X = -Size.X/2 + x * stepSizeX + stepSizeX / 2;
			for (int y = 0; y < ProbeCount.Y; y++)
			{
				probePos.Y = -Size.Y / 2 + y * stepSizeY + stepSizeY / 2;
				for (int z = 0; z < ProbeCount.Z; z++)
				{
					probePos.Z = -Size.Z / 2 + z * stepSizeZ + stepSizeZ / 2;

					var finalPos = probePos;
					var probe = _probeScene.Instantiate() as AmvProbe;
					AddChild(probe);

					probe.BoundsPosition = new Vector3I(x, y, z);
					
					_probes.Add(probe);
					
					probe.Position = finalPos;
					probe.GlobalRotation = GlobalRotation;
					probeCounter++;
				}
			}
		}
	}

	int CellToIndex(Vector3I cell)
	{
		if (cell.X < ProbeCount.X || cell.X > ProbeCount.X ||
		    cell.Y < ProbeCount.Y || cell.Y > ProbeCount.Y ||
		    cell.Z < ProbeCount.Z || cell.Z > ProbeCount.Z) return -1;
		
		return cell.X + cell.Y * ProbeCount.Y + cell.Z * ProbeCount.Y * ProbeCount.Z;
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
		v.X = (int)n;
		ProbeCount = v;
		SizeChanged();
	}
	
	public void SetProbesZ(double n)
	{
		var v = ProbeCount;
		v.Y = (int)n;
		ProbeCount = v;
		SizeChanged();
	}
	
	public void SetProbesY(double n)
	{
		var v = ProbeCount;
		v.Z = (int)n;
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
