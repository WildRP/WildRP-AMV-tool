using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WildRP.AMVTool;
using WildRP.AMVTool.GUI;

public partial class AMVBaker : Node3D
{
	[Export] private Node3D _placeholder;
	[Export(PropertyHint.Layers3DPhysics)] private uint _rayMask = 1;
	[Export] private PackedScene _probeScene;
	
	private Node _modelRoot;
	
	public static AMVBaker Instance { get; private set; }

	private List<AMVProbe> _probes = new();
	
	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();
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
		
		List<Node> nodes = new();
		Utils.GetAllChildren(_modelRoot, nodes);

		List<Tuple<MeshInstance3D, StaticBody3D>> result = new();
		
		var meshes = nodes.OfType<MeshInstance3D>().ToList();
		foreach (var m in meshes)
		{
			var body = new StaticBody3D();
			body.DisableMode = CollisionObject3D.DisableModeEnum.Remove;
			body.CollisionLayer = 1;
			
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

	public void Bake()
	{

		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 4; y++)
			{
				for (int z = 0; z < 8; z++)
				{
				
					var p = _probeScene.Instantiate() as AMVProbe;
					AddChild(p);
					p.GlobalPosition = new Vector3(x - 4, y - 2, z - 4);
					_probes.Add(p);
				}
			}
		}
		
		foreach (var probe in _probes)
		{
			for (int i = 0; i < 512; i++)
			{
				probe.CaptureSample();
			}
			probe.UpdateAverage();
		}
	}
}
