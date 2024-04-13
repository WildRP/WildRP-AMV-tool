using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WildRP.AMVTool;
using WildRP.AMVTool.GUI;

namespace WildRPAMVTool.scripts;

public partial class DeferredProbeBaker : Node3D
{
	[Export] private SubViewport _renderViewport;
	[Export] private Camera3D _mainCamera;

    private Node _modelRoot;
    public static DeferredProbeBaker Instance;

    private Dictionary<string, DeferredProbe> _deferredProbes = [];
    public Dictionary<string, DeferredProbe> DeferredProbes => _deferredProbes;

    private List<DeferredProbe> _bakeQueue = [];
    
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

    private int _probeBakeCounter;
    public override void _Process(double delta)
    {
	    _renderViewport.RenderTargetUpdateMode =
		    _bakeQueue.Count != 0 ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
	    
	    // we have to render one image from one camera per frame... thanks godot
	    if (_bakeQueue.Count != 0)
	    {
		    var idx = _probeBakeCounter % _bakeQueue.Count;
		    var p = _bakeQueue[idx];
		    p.BakeNext();
		    _probeBakeCounter++;
	    }

	    if (_bakeQueue.All(p => p.Baked))
	    {
		    _bakeQueue.Clear();
		    _mainCamera.Current = true;
	    }
    }

    public void BakeAll()
    {
	    _bakeQueue.AddRange(_deferredProbes.Values);
	    _probeBakeCounter = 0;

	    foreach (var p in _bakeQueue)
	    {
		    p.Clear();
	    }
    }

    public void ExportAll()
    {
	    foreach (var p in _deferredProbes)
	    {
		    p.Value.GenerateTextures();
	    }
    }
    
    public void Clear()
    {
	    _deferredProbes.Clear();
    }

    public void RegisterProbe(DeferredProbe probe)
    {
	    DeferredProbes.Add(probe.GuiListName, probe);
	    probe.SetViewport(_renderViewport);
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
        _renderViewport.AddChild(_modelRoot);
        AddChild(_modelRoot.Duplicate());
		
        List<Node> nodes = [];
        Utils.GetAllChildren(_modelRoot, nodes);

        List<MeshInstance3D> result = [];
		
        var meshes = nodes.OfType<MeshInstance3D>().ToList();
        foreach (var m in meshes)
        {
	        m.Layers = 0;
	        m.SetLayerMaskValue(1, true);
	        m.SetLayerMaskValue(20, true);
			
            result.Add(m);
        }
		
        return result;
    }

    public DeferredProbe GetProbe(string name) => DeferredProbes[name];
    
}