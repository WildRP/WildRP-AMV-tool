using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WildRP.AMVTool;
using WildRP.AMVTool.GUI;

namespace WildRPAMVTool.scripts;

public partial class DeferredProbeBaker : Node3D
{
	[Export] private Camera3D _renderCamera;
	[Export] private SubViewport _renderViewport;

    private Node _modelRoot;
    public static DeferredProbeBaker Instance;

    private Dictionary<string, DeferredProbe> _deferredProbes = [];

    public Dictionary<string, DeferredProbe> DeferredProbes => _deferredProbes;
    
    public override void _Ready()
    {
	    if (Instance == null)
	    {
		    Instance = this;
		    DeferredProbesUi.GuiToggled += b => Visible = b;
	    }
	    else
	    {
		    QueueFree();
	    }
    }

    public void Clear()
    {
	    _deferredProbes.Clear();
    }

    public void RegisterProbe(DeferredProbe probe)
    {
	    DeferredProbes.Add(probe.GuiListName, probe);
	    probe.Deleted += volume => DeferredProbes.Remove(probe.GuiListName);
    }
    
    public List<MeshInstance3D> LoadModel(string path)
    {
        _modelRoot?.QueueFree();

        if (path == "") return null;
        
        var modelDoc = new GltfDocument();
        var modelState = new GltfState();
        modelState.CreateAnimations = false;

        var error = modelDoc.AppendFromFile(path, modelState);
        if (error != Error.Ok) return null;

        modelState.Lights.Clear();
        modelState.Cameras.Clear();
		
        _modelRoot = modelDoc.GenerateScene(modelState);
        AddChild(_modelRoot);
		
        List<Node> nodes = [];
        Utils.GetAllChildren(_modelRoot, nodes);

        List<MeshInstance3D> result = [];
		
        var meshes = nodes.OfType<MeshInstance3D>().ToList();
        foreach (var m in meshes)
        {
	        m.Layers = 0;
	        m.SetLayerMaskValue(1, true);
	        m.SetLayerMaskValue(20, true);
	        
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
            m.AddChild(body);
			
            result.Add(m);
        }
		
        return result;
    }

    public DeferredProbe GetProbe(string name) => DeferredProbes[name];
    
}