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

	[Export] private ShaderMaterial _probeMeshMaterial;
	[Export] private ShaderMaterial _decalMeshMaterial;

    private Node _modelRoot;
    private Node3D _visibleModelRoot;
    public static DeferredProbeBaker Instance;

    private Dictionary<string, DeferredProbe> _deferredProbes = [];
    public Dictionary<string, DeferredProbe> DeferredProbes => _deferredProbes;

    private List<DeferredProbe> _bakeQueue = [];
    private List<MeshInstance3D> _renderMeshes = [];
    
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
    
    public List<Tuple<MeshInstance3D, MeshInstance3D>> LoadModel(string path)
    {
        _modelRoot?.QueueFree();
        _visibleModelRoot?.QueueFree();
        _renderMeshes.Clear();
        
        _visibleModelRoot = new Node3D();
        AddChild(_visibleModelRoot);

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
		
        List<Node> nodes = [];
        Utils.GetAllChildren(_modelRoot, nodes);

        List<Tuple<MeshInstance3D, MeshInstance3D>> result = [];
		
        var meshes = nodes.OfType<MeshInstance3D>().ToList();
        foreach (var m in meshes)
        {
	        var s = m.GetSurfaceOverrideMaterialCount();
	        for (int i = 0; i < s; i++)
	        {
		        m.SetLayerMaskValue(20, true);
		        var mat = m.Mesh.SurfaceGetMaterial(i) as StandardMaterial3D;

		        ShaderMaterial newMat;
		        
		        if (mat.Transparency != BaseMaterial3D.TransparencyEnum.Disabled)
					newMat = _decalMeshMaterial.Duplicate() as ShaderMaterial;
		        else
					newMat = _probeMeshMaterial.Duplicate() as ShaderMaterial;
		        
		        if (mat.AlbedoTexture != null) newMat.SetShaderParameter("ab", mat.AlbedoTexture.Duplicate());
		        
		        m.SetSurfaceOverrideMaterial(i, newMat);
	        }

	        var visibleMesh = m.Duplicate() as MeshInstance3D;
	        _visibleModelRoot.AddChild(visibleMesh);
	        
	        _renderMeshes.Add(m);

	        var res = new Tuple<MeshInstance3D, MeshInstance3D>(m, visibleMesh);
            result.Add(res);
        }
		
        return result;
    }

    public DeferredProbe GetProbe(string name) => DeferredProbes[name];
    
}