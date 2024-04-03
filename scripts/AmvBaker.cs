using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WildRP.AMVTool;
using WildRP.AMVTool.Autoloads;

public partial class AmvBaker : Node3D
{
	[Export] private Node3D _placeholder;
	[Export(PropertyHint.Layers3DPhysics)] private uint _rayMask = 1;
	[Export] private PackedScene _probeScene;
	
	private Node _modelRoot;
	private readonly Dictionary<string, AmbientMaskVolume> _ambientMaskVolumes = new();
	
	public static AmvBaker Instance { get; private set; }

	private Dictionary<string, AmbientMaskVolume> AmbientMaskVolumes => _ambientMaskVolumes;

	private List<AmbientMaskVolume> _bakeQueue = new();

	public bool BakeInProgress => _bakeQueue.Count > 0;

	public event Action BakeFinished;
	public event Action<float> UpdatePercentage;

	private int totalSamples = 1;
	private int bakedSamples = 0;

	public override void _PhysicsProcess(double delta)
	{
		if (_bakeQueue.Count > 0)
		{
			for (int i = _bakeQueue.Count - 1; i >= 0; i--)
			{
				var amv = _bakeQueue[i];
				if (amv.Baked)
				{
					GD.Print("BAKED!");
					amv.UpdateAverages(true);
					_bakeQueue.Remove(amv);
				}
				else
				{
					amv.CaptureSample();
					
					if (bakedSamples % 8 == 0) amv.UpdateAverages();
					
					bakedSamples++;
				}
			}

			UpdatePercentage((float)bakedSamples / totalSamples);
			// Cleared the queue
			if (_bakeQueue.Count == 0)
			{
				BakeFinished();
			}
		}
		
	}
	
	public void CancelBake()
	{
		foreach (var amv in _bakeQueue)
		{
			amv.UpdateAverages(true);
		}
		
		_bakeQueue.Clear();
		BakeFinished();
	}
	
	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();
	}

	public void Clear()
	{
		_ambientMaskVolumes.Clear();
	}

	public void GenerateTextures()
	{
		var xmlFile = new StringBuilder();
		foreach (var volumes in _ambientMaskVolumes)
		{
			volumes.Value.GenerateTextures();
			xmlFile.Append(volumes.Value.GetXml());
		}
		
		using var f = FileAccess.Open($"{SaveManager.GetProjectPath()}/put_these_in_your_amv_zone.xml", FileAccess.ModeFlags.Write);
			f.StoreString(xmlFile.ToString());
	}
	
	public (Error, List<Tuple<MeshInstance3D, StaticBody3D>>) LoadModel(string path)
	{
		if (path.EndsWith(".glb") == false) return (Error.InvalidParameter, null);

		_modelRoot?.QueueFree();
		_placeholder.Visible = false;

		var modelDoc = new GltfDocument();
		var modelState = new GltfState();
		modelState.CreateAnimations = false;

		var error = modelDoc.AppendFromFile(path, modelState);
		if (error != Error.Ok) return (error, null);

		modelState.Lights.Clear();
		modelState.Cameras.Clear();
		
		_modelRoot = modelDoc.GenerateScene(modelState);
		AddChild(_modelRoot);
		
		List<Node> nodes = [];
		Utils.GetAllChildren(_modelRoot, nodes);

		List<Tuple<MeshInstance3D, StaticBody3D>> result = [];
		
		var meshes = nodes.OfType<MeshInstance3D>().ToList();
		foreach (var m in meshes)
		{
			var body = new StaticBody3D();
			body.DisableMode = CollisionObject3D.DisableModeEnum.Remove;
			body.CollisionLayer = 1;
			body.InputRayPickable = false;
			
			var shape = new CollisionShape3D();
			var polygonShape = new ConcavePolygonShape3D();
			polygonShape.BackfaceCollision = true;
			polygonShape.Data = m.Mesh.GetFaces();

			shape.Shape = polygonShape;
			body.AddChild(shape);
			AddChild(body);
			
			result.Add(new Tuple<MeshInstance3D, StaticBody3D>(m, body));
		}
		
		return (error, result);
	}

	public void RegisterAmv(AmbientMaskVolume amv)
	{
		AmbientMaskVolumes.Add(amv.GuiListName, amv);
		amv.Deleted += volume => AmbientMaskVolumes.Remove(volume.GuiListName);
	}

	public void UpdateProjectAmvs()
	{
		foreach (var v in _ambientMaskVolumes)
		{
			SaveManager.UpdateAmv(v.Key, v.Value.Save());
		}
	}
	
	public void BakeAll()
	{
		foreach (var v in _ambientMaskVolumes)
		{
			if (v.Value.IncludeInFullBake == false) continue;
			
			v.Value.ClearSamples();
			_bakeQueue.Add(v.Value);
		}

		bakedSamples = 0;
		totalSamples = _bakeQueue.Count * GetSampleCount();
	}
	
	public AmbientMaskVolume GetVolume(string name)
	{
		return AmbientMaskVolumes[name];
	}

	public static int GetSampleCount() => 1 << Settings.SampleCount;
}
