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
using FileAccess = Godot.FileAccess;
using Image = Godot.Image;

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
    private int _bakeCounter;
    private int _bakeSteps = 33;

    public new bool Baked => _colorTextures.Count >= 6
                             && _normalTextures.Count >= 6
                             && _depthTextures.Count >= 6
                             && _skyMaskTextures.Count >= 6
                             && _occlusionTextures.Count >= 6;

    public bool Exported { get; private set; }

    public float BakeProgress => (float)_bakeCounter / _bakeSteps;
    
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
            img.WriteToFile($"{gdir}/Color_{i}.png");
            colorList.Add($"{gdir}/Color_{i}.png");
        }
        
        // we have to correct the vectors in post because godot doesnt really let me do rendering to textures properly
        for (int i = 0; i < _normalTextures.Count; i++)
        {
            _normalTextures[i].SrgbToLinear();
            var img = new SimpleImageIO.Image(size.X, size.Y, 4);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    var col = _normalTextures[i].GetPixel(x, y);
                    var a = _skyMaskTextures[i].GetPixel(x, y).Luminance;
                    
                    //_normalTextures[i].SetPixel(x, y, v.ToColor());
                    img.SetPixelChannel(x, y, 0, Mathf.Pow(col.R, 2.2f));
                    img.SetPixelChannel(x, y, 1, Mathf.Pow(col.G, 2.2f));
                    img.SetPixelChannel(x, y, 2, Mathf.Pow(col.B, 2.2f));
                    img.SetPixelChannel(x, y, 3, a);
                }
            }
            img.WriteToFile($"{gdir}/Normal_{i}.png");
            normalList.Add($"{gdir}/Normal_{i}.png");
        }
        
        for (int i = 0; i < _depthTextures.Count; i++)
        {
            var img = new SimpleImageIO.MonochromeImage(size.X, size.Y);
            for (int x = 0; x < size.X; x++)
            {
                for (int y = 0; y < size.Y; y++)
                {
                    var val = _depthTextures[i].GetPixel(x, y).R;
                    val *= 250;

                    // turn linear depth into logarithmic depth
                    var c = 0.01f;
                    val = Mathf.Log(val + c) / Mathf.Log(150 + c);
                    
                    img.AtomicAdd(x,y, val);
                }
            }
            
            img.WriteToFile($"{gdir}/Depth_{i}.hdr");
            depthList.Add($"./{Guid}/Depth_{i}.hdr");
        }
        
        using var f0 = FileAccess.Open($"{gdir}/color.txt", FileAccess.ModeFlags.Write);
            colorList.ForEach(s => f0.StoreLine(s));
            
        using var f1 = FileAccess.Open($"{gdir}/normal.txt", FileAccess.ModeFlags.Write);
            normalList.ForEach(s => f1.StoreLine(s));
        
        using var f2 = FileAccess.Open($"{gdir}/depth.txt", FileAccess.ModeFlags.Write);
            depthList.ForEach(s => f2.StoreLine(s));

        var tn = TextureName();

        
        // every probe comes in different resolution sizes from 1024 to 128
        // so there will be lots of resizing here

        string ul_path = $"{SaveManager.GetGlobalizedProjectPath()}\\{tn}_ul\\"; // 1024
        string hi_path = $"{SaveManager.GetGlobalizedProjectPath()}\\{tn}_hi\\"; // 512
        string std_path = $"{SaveManager.GetGlobalizedProjectPath()}\\{tn}\\"; // 256
        string lo_path = $"{SaveManager.GetGlobalizedProjectPath()}\\{tn}_lo\\"; // 128

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
                var p = Tex.Conv($"{ul_path}{tn}_0.dds", hi_path, size: 512, srgb: true);
                p.Run();
                
                p.Exited += () =>
                { 
                    var p = Tex.Conv($"{ul_path}{tn}_0.dds", std_path, size: 256, srgb: true);
                    p.Run();
                    
                    p.Exited += () =>
                    { 
                        var p = Tex.Conv($"{ul_path}{tn}_0.dds", lo_path, size: 128, srgb: true);
                        p.Run();
                        p.Exited += FinishExport;
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
                var p = Tex.Conv($"{ul_path}{tn}_1.dds", hi_path, size: 512);
                p.Run();
                
                p.Exited += () =>
                {
                    var p = Tex.Conv($"{ul_path}{tn}_1.dds", std_path, size: 256);
                    p.Run();
                    
                    p.Exited += () =>
                    {
                        var p = Tex.Conv($"{ul_path}{tn}_1.dds", lo_path, size: 128);
                        p.Run();
                        p.Exited += FinishExport;
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
                var p = Tex.Conv($"{ul_path}{tn}_d.dds", hi_path, size: 512, compress: false);
                p.Run();
                
                p.Exited += () =>
                {
                    var p = Tex.Conv($"{ul_path}{tn}_d.dds", std_path, size: 256, compress: false);
                    p.Run();
                    
                    p.Exited += () =>
                    {
                        var p = Tex.Conv($"{ul_path}{tn}_d.dds", lo_path, size: 128, compress: false);
                        p.Run();
                        p.Exited += FinishExport;
                    };
                };
            };
        };
        
        tx0.Run();
        tx1.Run();
        txd.Run();
        
    }

    private int _exportCount = 0;
    void FinishExport()
    {
        _exportCount++;
        _bakeCounter++;
        if (_exportCount < 3) return;
        
        Exported = true;
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
        
        /*
         * <Item>
           <minExtents x="-8.42226" y="-4.802952" z="4.185595" /> 
           <maxExtents x="-2.6022608" y="4.8970537" z="7.122675" />
           <rotation x="0" y="0" z="0" w="1" />
           <centerOffset x="0" y="0" z="0" />
           <influenceExtents x="1" y="0.95" z="0.9" />
           <probePriority value="255" />
           <guid value="0x4285A9A3EB917D74" />
          </Item>
         */
        var minExtents = GlobalPosition - Size/2;
        var maxExtents = GlobalPosition + Size/2;
        var rotation = Quaternion.FromEuler(new Vector3(0, 0, -RotationDegrees.Y));
        
        return new XDocument(
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
        _skyMaskTextures.Clear();
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