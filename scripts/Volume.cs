using System;
using System.Text.Json.Serialization;
using Godot;

namespace WildRP.AMVTool;

// Base class for AMVs and Deferred Probes
public partial class Volume : Node3D
{
    public string GuiListName { get; protected set; }
    public void Setup(string name) => GuiListName = name;
    public bool Baked
    {
        get;
        protected set;
    }
    
    protected Vector3 _size = Vector3.One;
    public Vector3 Size
    {
        get => _size;
        protected set => _size = value;
    }
    
    public event Action<Volume> Deleted;
    public event Action SizeChanged;

    public event Action<bool> UiToggled;

    protected void OnDeleted() => Deleted?.Invoke(this);
    protected void OnSizeChanged() => SizeChanged?.Invoke();

    protected void OnUiToggled(bool v) => UiToggled?.Invoke(v);
    
    public void ChangeSizeWithGizmo(Vector3 diff, bool positive)
    {
		
        _size += diff;
		
        if (positive)
            diff *= -1;
		
        Position -= Basis * (diff / 2);
		
        OnSizeChanged();
    }

    public class VolumeData
    {
        [JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
        public Vector3 Size = Vector3.One;
        [JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
        public Vector3 Position = Vector3.Zero;
    }
    
    #region UIConnectFunctions
    public void SetSizeX(double n)
    {
        var v = Size;
        v.X = (float)n;
        Size = v;
        OnSizeChanged();
    }
	
    public void SetSizeY(double n)
    {
        var v = Size;
        v.Y = (float)n;
        Size = v;
        OnSizeChanged();
    }
	
    public void SetSizeZ(double n)
    {
        var v = Size;
        v.Z = (float)n;
        Size = v;
        OnSizeChanged();
    }
	
    public void SetPositionX(double n)
    {
        var v = Position;
        v.X = (float)n;
        Position = v;
        OnSizeChanged();
    }
	
    public void SetPositionY(double n)
    {
        var v = Position;
        v.Y = (float)n;
        Position = v;
        OnSizeChanged();
    }
	
    public void SetPositionZ(double n)
    {
        var v = Position;
        v.Z = (float)n;
        Position = v;
        OnSizeChanged();
    }
    #endregion // UIConnectFunctions

    public virtual string GetXml() => "";

    public virtual void Delete()
    {
        QueueFree();
        OnDeleted();
    }

    public virtual bool Selected() => false;
}