using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WildRP.AMVTool;
using WildRP.AMVTool.GUI;

public partial class AmvBaker : Node3D
{
	[Export] private Node3D _placeholder;
	[Export(PropertyHint.Layers3DPhysics)] private uint _rayMask = 1;
	[Export] private PackedScene _probeScene;
	
	private Node _modelRoot;
	private Dictionary<string, AmbientMaskVolume> _ambientMaskVolumes = new();
	
	public static AmvBaker Instance { get; private set; }

	public Dictionary<string, AmbientMaskVolume> AmbientMaskVolumes => _ambientMaskVolumes;
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

	public AmbientMaskVolume GetVolume(string name)
	{
		return AmbientMaskVolumes[name];
	}
}
