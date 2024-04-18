using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WildRP.AMVTool;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool;

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
    private StandardMaterial3D _aoBakeMat;

    private VoxelGI _voxelGi;

    public static Action<float> UpdateBakeProgress;

    public enum BakePass
    {
	    Albedo = 0,
	    Normal,
	    Occlusion,
	    SkyMask,
	    Depth
    }
    
    public override void _Ready()
    {
	    if (Instance == null)
	    {
		    Instance = this;
		    DeferredProbesUi.GuiToggled += b => Visible = b;
		    _aoBakeMat = new StandardMaterial3D();
		    _aoBakeMat.Roughness = 1;
		    _aoBakeMat.AlbedoColor = Colors.White;
	    }
	    else
	    {
		    QueueFree();
	    }
    }

    private int _probeBakeCounter;
    private int _totalBakeSteps;
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
		    var progress = _bakeQueue.Average(probe => probe.BakeProgress);
		    UpdateBakeProgress.Invoke(progress);
	    }

	    if (_bakeQueue.All(p => p.Baked && p.Exported))
	    {
		    _bakeQueue.Clear();
		    _mainCamera.Current = true;
		    UpdateBakeProgress?.Invoke(1f);
	    }
    }

    public void BakeAll()
    {
	    _bakeQueue.AddRange(_deferredProbes.Values);
	    _probeBakeCounter = 0;

	    foreach (var m in _renderMeshes)
	    {
		    // TODO: hide decal/transparent meshes in AO bake pass
		    m.MaterialOverride = _aoBakeMat;
	    }
	    
	    foreach (var p in _bakeQueue)
	    {
		    p.Clear();
	    }
    }

    public void RequestPass(BakePass p)
    {
	    RenderingServer.GlobalShaderParameterSet("probe_rendering_pass", (int)p);
	    
	    switch (p)
	    {
		    case BakePass.Occlusion:
			    foreach (var m in _renderMeshes)
			    {
				    // TODO: hide decal/transparent meshes in AO bake pass
				    m.MaterialOverride = _aoBakeMat;
			    }
			    break;
		    case BakePass.Albedo:
		    case BakePass.Normal:
		    case BakePass.SkyMask:
		    case BakePass.Depth:
			    foreach (var m in _renderMeshes)
			    {
				    m.MaterialOverride = null;
			    }
			    break;
	    }
    }

    public void ExportAll()
    {
	    var xml = "";
	    foreach (var p in _deferredProbes)
	    {
		    p.Value.GenerateTextures();
		    xml += p.Value.GetXml();
	    }
	    
	    using var f = FileAccess.Open($"{SaveManager.GetProjectPath()}/probe_data.xml", FileAccess.ModeFlags.Write);
			f.StoreString(xml);
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
        _voxelGi?.QueueFree();
        
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
	        m.GIMode = GeometryInstance3D.GIModeEnum.Static;
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
		        if (mat.NormalTexture != null) newMat.SetShaderParameter("nm", mat.NormalTexture.Duplicate());
		        
		        m.SetSurfaceOverrideMaterial(i, newMat);
	        }

	        var visibleMesh = m.Duplicate() as MeshInstance3D;
	        visibleMesh.GIMode = GeometryInstance3D.GIModeEnum.Disabled;
	        _visibleModelRoot.AddChild(visibleMesh);
	        
	        _renderMeshes.Add(m);

	        var res = new Tuple<MeshInstance3D, MeshInstance3D>(m, visibleMesh);
            result.Add(res);
        }

        // Set up voxel GI
        _voxelGi = new VoxelGI();
        _voxelGi.Data = new VoxelGIData();
        _voxelGi.Data.Interior = false;
        _voxelGi.Data.NormalBias = 1f;
        _voxelGi.Data.Bias = 1f;
        _voxelGi.Subdiv = VoxelGI.SubdivEnum.Subdiv256;
        _voxelGi.Data.UseTwoBounces = true;
        _voxelGi.Data.Propagation = .33f;
        _voxelGi.Data.Energy = 1;
        _voxelGi.Data.DynamicRange = 2f;
		_renderViewport.AddChild(_voxelGi);
		_voxelGi.SetLayerMaskValue(20, true);

		var camSettings = new CameraAttributesPractical();
		camSettings.ExposureMultiplier = 1.2f;
		_voxelGi.CameraAttributes = camSettings;
        
        var avgpos = Vector3.Zero;
        var aabb = meshes[0].GlobalAabb();

        for (int i = 0; i < meshes.Count; i++)
        {
	        var m = meshes[i];
	        m.GIMode = GeometryInstance3D.GIModeEnum.Static;
	        avgpos += m.GlobalPosition;
	        aabb.Merge(m.GlobalAabb());
        }
        avgpos /= meshes.Count;
        _voxelGi.GlobalPosition = avgpos;
        _voxelGi.Size = aabb.Grow(2f).Size;
        
        foreach (var m in _renderMeshes)
        {
	        // TODO: hide decal/transparent meshes in AO bake pass
	        m.MaterialOverride = _aoBakeMat;
        }
        
        _voxelGi.Bake();
        
        foreach (var m in _renderMeshes)
        {
	        // TODO: hide decal/transparent meshes in AO bake pass
	        m.MaterialOverride = null;
        }
        
        return result;
    }
    
    public DeferredProbe GetProbe(string name) => DeferredProbes[name];

    public void RenameProbe(string oldname, string newname)
    {
	    var probe = GetProbe(oldname);
	    DeferredProbes.Remove(oldname);
	    DeferredProbes.Add(newname, probe);
	    SaveManager.RenameProbe(oldname, newname);
    }
}