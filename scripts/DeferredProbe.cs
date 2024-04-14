using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using Godot.Collections;
using WildRP.AMVTool.GUI;
using WildRPAMVTool.scripts;

namespace WildRP.AMVTool;

public partial class DeferredProbe : Volume
{
    public ulong Guid;
    public Vector3 InfluenceExtents;
    public Vector3 CenterOffset;

    [Export] private Node3D _centerNode;
    [Export] private Array<Camera3D> _renderCameras;

    private List<Image> _colorTextures = [];
    private List<Image> _normalTextures = [];
    private List<Image> _depthTextures = [];
    private List<Image> _skyMaskTextures = [];
    private List<Image> _occlusionTextures = [];
    
    private SubViewport _viewport;
    private Node _originalParent;

    public new bool Baked => _colorTextures.Count >= 6
                             && _normalTextures.Count >= 6
                             && _depthTextures.Count >= 6
                             && _skyMaskTextures.Count >= 6
                             && _occlusionTextures.Count >= 6;

    public override void _Ready()
    {
        base._Ready();
        SaveManager.SavingProject += SaveToProject;
        SizeChanged += UpdateCenter;
        _originalParent = GetParent();
    }

    public void SetViewport(SubViewport v)
    {
        _viewport = v;
    }
    
    public override void _ExitTree()
    {
        base._ExitTree();
        SaveManager.SavingProject -= SaveToProject;
    }

    void UpdateCenter()
    {
        _centerNode.Position = CenterOffset;
    }

    
    public void BakeNext()
    {
        if (Baked == false) Reparent(_viewport, true);
        
        if (_colorTextures.Count < 6)
            RenderColor(_colorTextures.Count);
        else if (_normalTextures.Count < 6)
            RenderNormal(_normalTextures.Count);
        else if (_skyMaskTextures.Count < 6)
            RenderSkymask(_skyMaskTextures.Count);
        else if (_depthTextures.Count < 6)
            RenderDepth(_depthTextures.Count);
        else if (_occlusionTextures.Count < 6)
            RenderOcclusion(_occlusionTextures.Count);
        
        if (Baked) Reparent(_originalParent, true);
    }


    private List<Image> _renderList;
    private void RenderColor(int cam)
    {
        GD.Print($"Render Color {cam}");
        _renderCameras[cam].Current = true;
        DeferredProbeBaker.Instance.RequestPass(DeferredProbeBaker.BakePass.Albedo);
        _renderList = _colorTextures;
        RenderingServer.FramePostDraw += GrabTex; // lol this is so silly. thanks godot.
    }
    
    private void RenderNormal(int cam)
    {
        GD.Print($"Render Normal {cam}");
        _renderCameras[cam].Current = true;
        DeferredProbeBaker.Instance.RequestPass(DeferredProbeBaker.BakePass.Normal);
        _renderList = _normalTextures;
        RenderingServer.FramePostDraw += GrabTex;
    }
    
    private void RenderSkymask(int cam)
    {
        GD.Print($"Render Skymask {cam}");
        _renderCameras[cam].Current = true;
        DeferredProbeBaker.Instance.RequestPass(DeferredProbeBaker.BakePass.SkyMask);
        _renderList = _skyMaskTextures;
        RenderingServer.FramePostDraw += GrabTex;
    }
    
    private void RenderDepth(int cam)
    {
        GD.Print($"Render Depth {cam}");
        _renderCameras[cam].Current = true;
        DeferredProbeBaker.Instance.RequestPass(DeferredProbeBaker.BakePass.Depth);
        _renderList = _depthTextures;
        RenderingServer.FramePostDraw += GrabTex;
    }
    
    private void RenderOcclusion(int cam)
    {
        GD.Print($"Render Occlusion {cam}");
        _renderCameras[cam].Current = true;
        DeferredProbeBaker.Instance.RequestPass(DeferredProbeBaker.BakePass.Occlusion);
        _renderList = _occlusionTextures;
        RenderingServer.FramePostDraw += GrabTex;
    }

    private void GrabTex()
    {
        var tex = _viewport.GetTexture().GetImage();
        _renderList.Add(tex);
        RenderingServer.FramePostDraw -= GrabTex;
    }

    public void GenerateTextures()
    {
        if (Baked == false) return;

        var dir = SaveManager.GetProjectPath() + "/" + Guid;
        var gdir = SaveManager.GetGlobalizedProjectPath() + "/" + Guid;
        if (DirAccess.DirExistsAbsolute(dir) == false)
            DirAccess.MakeDirAbsolute(dir);
        
        for (int i = 0; i < _colorTextures.Count; i++)
        {
            var err = _colorTextures[i].SavePng($"{dir}/Color_{i}.png");
            GD.Print(err);
        }
        
        // we have to correct the vectors in post because godot doesnt really let me do rendering to textures properly
        for (int i = 0; i < _normalTextures.Count; i++)
        {
            
            var size = _normalTextures[i].GetSize();
            _normalTextures[i].SrgbToLinear();
            var img = new SimpleImageIO.Image(size.X, size.Y, 3);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    var col = _normalTextures[i].GetPixel(x, y);
                    
                    //_normalTextures[i].SetPixel(x, y, v.ToColor());
                    img.SetPixelChannel(x, y, 0, col.R);
                    img.SetPixelChannel(x, y, 1, col.G);
                    img.SetPixelChannel(x, y, 2, col.B);
                }
            }
            img.ApplyOpInPlace(f => Mathf.Pow(f, 2.2f));
            img.WriteToFile($"{gdir}/Normal_{i}.png");
            //var err = _normalTextures[i].SavePng($"{dir}/Normal_{i}.png");
            //GD.Print(err);
        }
        
        for (int i = 0; i < _depthTextures.Count; i++)
        {
            var err = _depthTextures[i].SavePng($"{dir}/Depth_{i}.png");
            GD.Print(err);
        }
        
        for (int i = 0; i < _skyMaskTextures.Count; i++)
        {
            var err = _skyMaskTextures[i].SavePng($"{dir}/Skymask_{i}.png");
            GD.Print(err);
        }
        
        for (int i = 0; i < _occlusionTextures.Count; i++)
        {
            var err = _occlusionTextures[i].SavePng($"{dir}/Occlusion_{i}.png");
            GD.Print(err);
        }
    }
    
    public void Clear()
    {
        _colorTextures.Clear();
        _normalTextures.Clear();
        _skyMaskTextures.Clear();
        _depthTextures.Clear();
    }

    public void Load(KeyValuePair<string, DeferredProbeData> data)
    {
        GuiListName = data.Key;
        Guid = data.Value.Guid;
        Position = data.Value.Position;
        CenterOffset = data.Value.CenterOffset;
        Size = data.Value.Size;
        InfluenceExtents = data.Value.InfluenceExtents;

        var rot = RotationDegrees;
        rot.Y = data.Value.Rotation;
        RotationDegrees = rot;
    }

    private void SaveToProject() => SaveManager.UpdateDeferredProbe(GuiListName,Save());
    
    public DeferredProbeData Save()
    {
        var data = new DeferredProbeData();
		
        data.Guid = Guid;
        data.Position = Position;
        data.CenterOffset = CenterOffset;
        data.Size = Size;
        data.InfluenceExtents = InfluenceExtents;
        data.Rotation = RotationDegrees.Y;

        return data;
    }

    public override bool Selected() => DeferredProbesUi.SelectedProbe == this;

    public class DeferredProbeData : VolumeData
    {
        [JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
        public Vector3 CenterOffset;
        
        [JsonInclude]
        public ulong Guid;

        [JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
        public Vector3 InfluenceExtents;
    }
    
    #region UIConnectFunctions
    
    public void SetExtentsX(double n)
    {
        var v = InfluenceExtents;
        v.X = (int)n;
        InfluenceExtents = v;
    }
	
    public void SetExtentsZ(double n)
    {
        var v = InfluenceExtents;
        v.Y = (int)n;
        InfluenceExtents = v;
    }
	
    public void SetExtentsY(double n)
    {
        var v = InfluenceExtents;
        v.Z = (int)n;
        InfluenceExtents = v;
    }
    
    public void SetCenterOffsetX(double n)
    {
        var v = CenterOffset;
        v.X = (float)n;
        CenterOffset = v;
        OnSizeChanged();
    }
	
    public void SetCenterOffsetY(double n)
    {
        var v = CenterOffset;
        v.Y = (float)n;
        CenterOffset = v;
        OnSizeChanged();
    }
	
    public void SetCenterOffsetZ(double n)
    {
        var v = CenterOffset;
        v.Z = (float)n;
        CenterOffset = v;
        OnSizeChanged();
    }
    
    #endregion
}