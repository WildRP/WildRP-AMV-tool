using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool;

public partial class DeferredProbe : Volume
{
    public uint Uuid;
    public Vector3 InfluenceExtents;

    public override void _Ready()
    {
        base._Ready();
        SaveManager.SavingProject += SaveToProject;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        SaveManager.SavingProject -= SaveToProject;
    }

    public void Load(KeyValuePair<string, DeferredProbeData> data)
    {
        GuiListName = data.Key;
        Uuid = data.Value.Uuid;
        Position = data.Value.Position;
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
		
        data.Uuid = Uuid;
        data.Position = Position;
        data.Size = Size;
        data.InfluenceExtents = InfluenceExtents;
        data.Rotation = RotationDegrees.Y;

        return data;
    }

    public override bool Selected() => DeferredProbesUi.SelectedProbe == this;

    public class DeferredProbeData : VolumeData
    {
        [JsonInclude]
        public uint Uuid;

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
    
    #endregion
}