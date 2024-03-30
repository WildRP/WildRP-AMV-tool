using System.Collections.Generic;
using Godot;

namespace WildRP.AMVTool.Sceneview;

public partial class AmvBoundsMesh : MeshInstance3D
{
    [Export] private Color _color = Colors.Firebrick;
    [Export] private Node3D _planesNode;
    [Export] private PackedScene _boundsPlaneScene;

    private ImmediateMesh _mesh;
    private StandardMaterial3D _material;
    private AmbientMaskVolume _parentVolume;
    
    public override void _Ready()
    {
        _mesh = new ImmediateMesh();
        Mesh = _mesh;
        SetupMaterial();
        CreateMesh();
        
        _parentVolume = GetParent<AmbientMaskVolume>();

        for (int i = 0; i < 6; i++)
        {
            var plane = _boundsPlaneScene.Instantiate() as BoundsPlane;
            plane.Setup(_parentVolume, (BoundsPlane.PlaneDirection) i);
            _planesNode.AddChild(plane);
        }
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

    private void CreateMesh()
    {
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _material);
        
        var frontLeft = new Vector3(-.5f, -.5f, -.5f);
        var frontRight = new Vector3(.5f, -.5f, -.5f);
        var backLeft = new Vector3(-.5f, -.5f, .5f);
        var backRight = new Vector3(.5f, -.5f, .5f);
        
        // Draw bottom square
        DrawLine(frontLeft, frontRight);
        DrawLine(frontRight, backRight);
        DrawLine(backRight, backLeft);
        DrawLine(backLeft, frontLeft);

        frontLeft += Vector3.Up;
        frontRight += Vector3.Up;
        backLeft += Vector3.Up;
        backRight += Vector3.Up;
        
        // Top square
        DrawLine(frontLeft, frontRight);
        DrawLine(frontRight, backRight);
        DrawLine(backRight, backLeft);
        DrawLine(backLeft, frontLeft);
        
        // Pillars
        DrawLine(frontLeft, frontLeft + Vector3.Down);
        DrawLine(frontRight, frontRight + Vector3.Down);
        DrawLine(backRight, backRight + Vector3.Down);
        DrawLine(backLeft, backLeft + Vector3.Down);
        
        _mesh.SurfaceEnd();
    }
}