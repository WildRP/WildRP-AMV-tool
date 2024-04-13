using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using Godot.Collections;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool;

public partial class DeferredProbe : Volume
{
    public ulong Guid;
    public Vector3 InfluenceExtents;
    public Vector3 CenterOffset;

    [Export] private Node3D _centerNode;
    [Export] private Array<Camera3D> _renderCameras;

    public List<Image> ColorTextures = [];
    private SubViewport _viewport;
    private Node _originalParent;

    public bool Baked => ColorTextures.Count >= 6;

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
        
        if (ColorTextures.Count < 6)
            RenderColor(ColorTextures.Count);
        
        if (Baked) Reparent(_originalParent, true);
    }
    
    public void RenderColor(int cam)
    {
        _renderCameras[cam].Current = true;
        RenderingServer.FramePostDraw += GrabColorTex; // lol this is so silly. thanks godot.
    }

    public void GrabColorTex()
    {
        var tex = _viewport.GetTexture().GetImage();
        ColorTextures.Add(tex);
        RenderingServer.FramePostDraw -= GrabColorTex;
    }

    public void GenerateTextures()
    {
        if (Baked == false) return;

        var dir = SaveManager.GetProjectPath() + "/" + Guid;
        if (DirAccess.DirExistsAbsolute(dir) == false)
            DirAccess.MakeDirAbsolute(dir);
        
        for (int i = 0; i < ColorTextures.Count; i++)
        {
            var err = ColorTextures[i].SavePng($"{dir}/Color_{i}.png");
            GD.Print(err);
        }
    }
    
    public void Clear()
    {
        ColorTextures.Clear();
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