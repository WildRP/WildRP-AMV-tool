using Godot;

namespace WildRP.AMVTool.Sceneview;

public partial class AmvBounds : MeshInstance3D
{
    [Export] private bool _drawPoints = false;
    [Export] private Color _color = Colors.Firebrick;

    private ImmediateMesh _mesh;
    private Aabb _bounds;
    private StandardMaterial3D _material;
    
    public override void _Ready()
    {
        _mesh = new ImmediateMesh();
        Mesh = _mesh;
        _bounds = new Aabb(Vector3.Zero, Vector3.One * 4);
        SetupMaterial();
        UpdateBounds();
    }

    private void SetupMaterial()
    {
        _material = new StandardMaterial3D();
        _material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        _material.AlbedoColor = _color;
        _material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        
        _material.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled;
        _material.NoDepthTest = false;
        _material.RenderPriority = 5;

        var backside = _material.Duplicate() as StandardMaterial3D;
        backside.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Always;
        backside.NoDepthTest = true;
        backside.RenderPriority = 4;
        backside.AlbedoColor *= new Color(1, 1, 1, 0.15f);
        _material.NextPass = backside;
    }

    private void DrawLine(Vector3 from, Vector3 to)
    {
        _mesh.SurfaceAddVertex(from);
        _mesh.SurfaceAddVertex(to);
    }

    private void UpdateBounds()
    {
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _material);

        var c = _bounds.GetCenter();
        
        DrawLine(_bounds.GetEndpoint(3) - c, _bounds.GetEndpoint(7) - c);
        DrawLine(_bounds.GetEndpoint(3) - c, _bounds.GetEndpoint(2) - c);
        DrawLine(_bounds.GetEndpoint(3) - c, _bounds.GetEndpoint(1) - c);
        DrawLine(_bounds.GetEndpoint(4) - c, _bounds.GetEndpoint(0) - c);
        DrawLine(_bounds.GetEndpoint(4) - c, _bounds.GetEndpoint(6) - c);
        DrawLine(_bounds.GetEndpoint(4) - c, _bounds.GetEndpoint(5) - c);
        DrawLine(_bounds.GetEndpoint(0) - c, _bounds.GetEndpoint(1) - c);
        DrawLine(_bounds.GetEndpoint(2) - c, _bounds.GetEndpoint(6) - c);
        DrawLine(_bounds.GetEndpoint(7) - c, _bounds.GetEndpoint(6) - c);
        DrawLine(_bounds.GetEndpoint(7) - c, _bounds.GetEndpoint(5) - c);
        DrawLine(_bounds.GetEndpoint(0) - c, _bounds.GetEndpoint(2) - c);
        DrawLine(_bounds.GetEndpoint(5) - c, _bounds.GetEndpoint(1) - c);
        
        _mesh.SurfaceEnd();
    }
}