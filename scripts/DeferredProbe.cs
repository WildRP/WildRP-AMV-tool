using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Godot;
using Godot.Collections;
using StbImageWriteSharp;
using WildRP.AMVTool.GUI;
using WildRP.AMVTool;
using WildRP.AMVTool.Autoloads;
using FileAccess = Godot.FileAccess;
using Image = Godot.Image;

namespace WildRP.AMVTool;

public partial class DeferredProbe : Volume
{
    public ulong Guid;
    public Vector3 InfluenceExtents = Vector3.One;
    public Vector3 CenterOffset = Vector3.Zero;

    [Export] private Node3D _centerNode;
    [Export] private Array<Camera3D> _renderCameras;

    private List<Image> _colorTextures = [];
    private List<Image> _normalTextures = [];
    private List<Image> _depthTextures = [];
    private List<Image> _windowMaskTextures = [];
    private List<Image> _occlusionTextures = [];
    
    private SubViewport _viewport;
    private Node _originalParent;
    private int _bakeCounter;
    private int _bakeSteps = 33;

    public new bool Baked => _colorTextures.Count >= 6
                             && _normalTextures.Count >= 6
                             && _depthTextures.Count >= 6
                             && _windowMaskTextures.Count >= 6
                             && _occlusionTextures.Count >= 6;

    public bool Exported { get; private set; }

    public float BakeProgress => (float)_bakeCounter / _bakeSteps;
    
    public override void _Ready()
    {
        base._Ready();
        SaveManager.SavingProject += SaveToProject;
        SizeChanged += UpdateCenter;
        _originalParent = GetParent();
        
        TreeExiting += () =>
        {
            SaveManager.SavingProject -= SaveToProject;
            AmvBakerGui.GuiToggled -= OnUiToggled;
        };
    }

    public void SetViewport(SubViewport v)
    {
        _viewport = v;
    }

    void UpdateCenter()
    {
        _centerNode.Position = CenterOffset;
    }

    public override void _Process(double delta)
    {
        _centerNode.GlobalRotation = Vector3.Zero;
    }

    public void BakeNext()
    {
        if (Baked == false) Reparent(_viewport, true);
        
        if (_colorTextures.Count < 6)
            RenderColor(_colorTextures.Count);
        else if (_normalTextures.Count < 6)
            RenderNormal(_normalTextures.Count);
        else if (_windowMaskTextures.Count < 6)
            RenderWindowMask(_windowMaskTextures.Count);
        else if (_depthTextures.Count < 6)
            RenderDepth(_depthTextures.Count);
        else if (_occlusionTextures.Count < 6)
            RenderOcclusion(_occlusionTextures.Count);

        _bakeCounter++;

        if (Baked)
        {
            Reparent(_originalParent, true);
            GenerateTextures();
        }
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
    
    private void RenderWindowMask(int cam)
    {
        GD.Print($"Render Window Mask {cam}");
        _renderCameras[cam].Current = true;
        DeferredProbeBaker.Instance.RequestPass(DeferredProbeBaker.BakePass.WindowMask);
        _renderList = _windowMaskTextures;
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

    private float GetPlaneDistance(int index)
    {
        var cam = _renderCameras[index];
        
        var minExtents = CenterOffset - Size/2;
        var maxExtents = CenterOffset + Size/2;
        //var depths = new[] {maxExtents.X, minExtents.X, maxExtents.Y, minExtents.Y, maxExtents.Z, minExtents.Z};
    
        var dir = cam.GlobalBasis.Z.Rotated(Vector3.Up, -GlobalRotationDegrees.Y).Normalized();

        var intersect = IntersectAABB(Vector3.Zero, dir, minExtents, maxExtents);

        return intersect.Y;
    }

    private Vector2 IntersectAABB(Vector3 rayOrigin, Vector3 rayDir, Vector3 boxMin, Vector3 boxMax) {
        Vector3 tMin = (boxMin - rayOrigin) / rayDir;
        Vector3 tMax = (boxMax - rayOrigin) / rayDir;
        Vector3 t1 = Utils.V3Min(tMin, tMax);
        Vector3 t2 = Utils.V3Max(tMin, tMax);
        float tNear = Mathf.Max(Mathf.Max(t1.X, t1.Y), t1.Z);
        float tFar = Mathf.Min(Mathf.Min(t2.X, t2.Y), t2.Z);
        return new Vector2(tNear, tFar);
    }
    
    public void GenerateTextures()
    {
        if (Baked == false || Exported) return;
        Exported = true;

        var dir = SaveManager.GetProjectPath() + "/" + Guid;
        if (DirAccess.DirExistsAbsolute(dir) == false)
            DirAccess.MakeDirAbsolute(dir);
        
        var size = _colorTextures[0].GetSize();
        
        List<string> colorList = [];
        List<string> normalList = [];
        List<string> depthList = [];
        
        for (int i = 0; i < _colorTextures.Count; i++)
        {
            var img = new SimpleImageIO.Image(size.X, size.Y, 4);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    var col = _colorTextures[i].GetPixel(x, y);
                    var a = _occlusionTextures[i].GetPixel(x, y).Luminance;
                    col = col.SrgbToLinear();
                    
                    img.SetPixelChannel(x, y, 0, col.R);
                    img.SetPixelChannel(x, y, 1, col.G);
                    img.SetPixelChannel(x, y, 2, col.B);
                    img.SetPixelChannel(x, y, 3, a);
                }
            }
            img.WriteToFile($"{dir}/Color_{i}.png");
            img.Dispose();
            colorList.Add($"{dir}/Color_{i}.png");
        }
        
        for (int i = 0; i < _normalTextures.Count; i++)
        {
            _normalTextures[i].SrgbToLinear();
            var img = new SimpleImageIO.Image(size.X, size.Y, 4);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    var col = _normalTextures[i].GetPixel(x, y);
                    var a = _windowMaskTextures[i].GetPixel(x, y).Luminance > .2f ? 1f : 0f;

                    Vector3 n = col.ToVector();
                    n = n * 2.0f - Vector3.One;
                    col = (n.Normalized() * 0.5f + Vector3.One*0.5f).ToColor().LinearToSrgb();
                    
                    //_normalTextures[i].SetPixel(x, y, v.ToColor());
                    img.SetPixelChannel(x, y, 0, col.R);
                    img.SetPixelChannel(x, y, 1, col.G);
                    img.SetPixelChannel(x, y, 2, col.B);
                    img.SetPixelChannel(x, y, 3, a);
                }
            }
            img.WriteToFile($"{dir}/Normal_{i}.png");
            img.Dispose();
            normalList.Add($"{dir}/Normal_{i}.png");
        }
        
        for (int i = 0; i < _depthTextures.Count; i++)
        {
            var planeDistance = GetPlaneDistance(i);
            var img = new SimpleImageIO.MonochromeImage(size.X, size.Y);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    var val = _depthTextures[i].GetPixel(x, y).R;

                    val *= 150f;

                    float depth = val > 149f ? 1.0f : 0.999999f;
                    
                    img.AtomicAdd(x,y, depth);
                }
            }
            
            img.WriteToFile($"{dir}/Depth_{i}.hdr");
            img.Dispose();
            depthList.Add($"./{Guid}/Depth_{i}.hdr");
        }
        
        using var f0 = FileAccess.Open($"{dir}/color.txt", FileAccess.ModeFlags.Write);
            colorList.ForEach(s => f0.StoreLine(s));
            
        using var f1 = FileAccess.Open($"{dir}/normal.txt", FileAccess.ModeFlags.Write);
            normalList.ForEach(s => f1.StoreLine(s));
        
        using var f2 = FileAccess.Open($"{dir}/depth.txt", FileAccess.ModeFlags.Write);
            depthList.ForEach(s => f2.StoreLine(s));

        var tn = TextureName();

        
        // every probe comes in different resolution sizes from 1024 to 128
        // so there will be lots of resizing here

        string ul_path = $"{SaveManager.GetProjectPath()}\\{tn}_ul\\"; // 1024
        string hi_path = $"{SaveManager.GetProjectPath()}\\{tn}_hi\\"; // 512
        string std_path = $"{SaveManager.GetProjectPath()}\\{tn}\\"; // 256
        string lo_path = $"{SaveManager.GetProjectPath()}\\{tn}_lo\\"; // 128

        DirAccess.MakeDirAbsolute(ul_path);
        DirAccess.MakeDirAbsolute(hi_path);
        DirAccess.MakeDirAbsolute(std_path);
        DirAccess.MakeDirAbsolute(lo_path);
        
        ul_path = $"./{tn}_ul/"; // 1024
        hi_path = $"./{tn}_hi/"; // 512
        std_path = $"./{tn}/"; // 256
        lo_path = $"./{tn}_lo/"; // 128
        
        // first make the base DDS files
        var tx0 = Tex.Assemble($"./{Guid}/color.txt", $"{ul_path}{tn}_0.dds",
            Tex.TextureFormat.R8G8B8A8_UNORM_SRGB);
        
        var tx1 = Tex.Assemble($"./{Guid}/normal.txt", $"{ul_path}{tn}_1.dds",
            Tex.TextureFormat.R8G8B8A8_UNORM);

        var txd = Tex.Assemble($"./{Guid}/depth.txt", $"{ul_path}{tn}_d.dds",
            Tex.TextureFormat.R16_UNORM);

        tx0.Exited += () =>
        { 
            var p = Tex.Conv($"{ul_path}{tn}_0.dds", ul_path, srgb: true);
                p.Run();
                
            p.Exited += () =>
            { 
                var p1 = Tex.Conv($"{ul_path}{tn}_0.dds", hi_path, size: 512, srgb: true);
                p1.Run();
                
                p1.Exited += () =>
                { 
                    var p2 = Tex.Conv($"{ul_path}{tn}_0.dds", std_path, size: 256, srgb: true);
                    p2.Run();
                    
                    p2.Exited += () =>
                    { 
                        var p3 = Tex.Conv($"{ul_path}{tn}_0.dds", lo_path, size: 128, srgb: true);
                        p3.Run();
                        p3.Exited += CleanupExport;
                    };
                };
            };
        };
        
        
        tx1.Exited += () =>
        {
            var p = Tex.Conv($"{ul_path}{tn}_1.dds", ul_path);
            p.Run();
            
            p.Exited += () =>
            {
                var p1 = Tex.Conv($"{ul_path}{tn}_1.dds", hi_path, size: 512);
                p1.Run();
                
                p1.Exited += () =>
                {
                    var p2 = Tex.Conv($"{ul_path}{tn}_1.dds", std_path, size: 256);
                    p2.Run();
                    
                    p2.Exited += () =>
                    {
                        var p3 = Tex.Conv($"{ul_path}{tn}_1.dds", lo_path, size: 128);
                        p3.Run();
                        p3.Exited += CleanupExport;
                    };
                };
            };
        };
        
        txd.Exited += () =>
        {
            var p = Tex.Conv($"{ul_path}{tn}_d.dds", ul_path, compress: false);
            p.Run();
            
            p.Exited += () =>
            {
                var p1 = Tex.Conv($"{ul_path}{tn}_d.dds", hi_path, size: 512, compress: false);
                p1.Run();
                
                p1.Exited += () =>
                {
                    var p2 = Tex.Conv($"{ul_path}{tn}_d.dds", std_path, size: 256, compress: false);
                    p2.Run();
                    
                    p2.Exited += () =>
                    {
                        var p3 = Tex.Conv($"{ul_path}{tn}_d.dds", lo_path, size: 128, compress: false);
                        p3.Run();
                        p3.Exited += CleanupExport;
                    };
                };
            };
        };
        
        tx0.Run();
        tx1.Run();
        txd.Run();
        
    }

    private int _exportCount = 0;
    void CleanupExport()
    {
        _exportCount++;
        _bakeCounter++;
        if (_exportCount < 3) return;
        
        _exportCount = 0;
        // Clean up files, leaving only the exported DDS files
        var path = $"{SaveManager.GetProjectPath()}/{Guid}";
        var files = DirAccess.GetFilesAt(path);

        foreach (var file in files)
        {
            DirAccess.RemoveAbsolute($"{path}/{file}");
        }

        DirAccess.RemoveAbsolute(path);
    }

    public override string GetXml()
    {
        // CenterOffset in the XML actually just seems to be the rotation point?
        // Which means that in practice the Z value of it does nothing
        
        // We use centeroffset to move the capture point relative to the bounding box though

        var rdrPos = Position;
        rdrPos.Z *= -1;

        var minExtents = rdrPos - Size/2;
        var maxExtents = rdrPos + Size/2;
        
        var rotation = Basis.GetRotationQuaternion();
        rotation.Z = rotation.Y;
        rotation.Y = 0;
        
        return new XDocument(
            new XComment(GuiListName),
            new XElement("Item",
                new XElement("minExtents", new XAttribute("x", minExtents.X), new XAttribute("y", minExtents.Z), new XAttribute("z", minExtents.Y)),
                new XElement("maxExtents", new XAttribute("x", maxExtents.X), new XAttribute("y", maxExtents.Z), new XAttribute("z", maxExtents.Y)),
                new XElement("rotation", new XAttribute("x", rotation.X), new XAttribute("y", rotation.Y), new XAttribute("z", rotation.Z), new XAttribute("w", rotation.W)),
                new XElement("centerOffset", new XAttribute("x", CenterOffset.X), new XAttribute("y", CenterOffset.Z), new XAttribute("z", CenterOffset.Y)),
                new XElement("influenceExtents", new XAttribute("x", InfluenceExtents.X), new XAttribute("y", InfluenceExtents.Z), new XAttribute("z", InfluenceExtents.Y)),
                new XElement("probePriority", new XAttribute("value", 255)),
                new XElement("guid", new XAttribute("value", "0x"+Guid.ToString("x16")))
                ))+"\r\n";
    }

    public void Clear()
    {
        _colorTextures.Clear();
        _normalTextures.Clear();
        _windowMaskTextures.Clear();
        _depthTextures.Clear();
        Exported = false;
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

    public void EnableDebugView()
    {
        _renderCameras[5].Current = true;
    }
    
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

    static (uint,uint) SplitGuid(string guid)
    {
        if (guid.StartsWith("0x")) guid = guid.Remove(0, 2);
        
        var firsthalf = Convert.ToUInt32("0x" + guid.Remove(8), 16);
        var secondhalf = Convert.ToUInt32("0x" + guid.Remove(0, 8), 16);
        return (firsthalf, secondhalf);
    }
    
    public string TextureName()
    {
        var (p1, p2) = SplitGuid(Guid.ToString("x16"));
        var p3 = Utils.JenkinsHash(SaveManager.CurrentProject.InteriorName);

        return Utils.LightProbeHash(new[] { p1, p2, p3 }).ToString();
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