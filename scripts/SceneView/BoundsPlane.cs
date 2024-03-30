using System;
using System.Drawing;
using Godot;

namespace WildRP.AMVTool.Sceneview;

public partial class BoundsPlane : MeshInstance3D
{
    [Serializable]
    public enum PlaneDirection
    {
        XPos = 0,
        XNeg,
        YPos,
        YNeg,
        ZPos,
        ZNeg
    }

    [Export] private PlaneDirection _planeDirection;
    [Export] private StaticBody3D _staticBody3D;
    [Export] private CollisionShape3D _collisionShape3D;
    
    private AmbientMaskVolume _volume;
    private QuadMesh _quadMesh;
    private BoxShape3D _colliderBox;
    private bool _hovered;

    private const float _minColliderSize = 0.01f;
    
    public void Setup(AmbientMaskVolume amv, PlaneDirection dir)
    {
        _volume = amv;
        _volume.SizeChanged += UpdatePlane;

        Mesh = Mesh.Duplicate() as Mesh;
        _quadMesh = Mesh as QuadMesh;

        _collisionShape3D.Shape = _collisionShape3D.Shape.Duplicate() as Shape3D;
        _colliderBox = _collisionShape3D.Shape as BoxShape3D;
        
        switch (dir)
        {
            case PlaneDirection.XPos:
            case PlaneDirection.XNeg:
                _quadMesh.Orientation = PlaneMesh.OrientationEnum.X;
                _colliderBox.Size = new Vector3(_minColliderSize, 1, 1);
                break;
            case PlaneDirection.YPos:
            case PlaneDirection.YNeg:
                _quadMesh.Orientation = PlaneMesh.OrientationEnum.Y;
                _colliderBox.Size = new Vector3(1, _minColliderSize, 1);
                break;
            case PlaneDirection.ZPos:
            case PlaneDirection.ZNeg:
                _quadMesh.Orientation = PlaneMesh.OrientationEnum.Z;
                _colliderBox.Size = new Vector3(1, 1, _minColliderSize);
                break;
        }

        if (dir is PlaneDirection.XNeg or PlaneDirection.YNeg or PlaneDirection.ZNeg)
            _quadMesh.FlipFaces = true;

        _planeDirection = dir;
        
        _staticBody3D.MouseEntered += () => _hovered = true;
        _staticBody3D.MouseExited += () => _hovered = false;
        
        UpdatePlane();
    }

    public override void _Process(double delta)
    {
        if (_volume.Selected)
            Transparency = _hovered ? 0f : 0.5f;
        else
            Transparency = .9f;
    }

    private void UpdatePlane()
    {
        var pos = Vector3.Zero;
        var boxSize = _volume.Size;
        
        switch (_planeDirection)
        {
            case PlaneDirection.XPos:
            case PlaneDirection.XNeg:
                _quadMesh.Size = new Vector2(_volume.Size.Z, _volume.Size.Y);
                pos.X += _volume.Size.X * (_planeDirection == PlaneDirection.XNeg ? -.5f : .5f);
                boxSize.X = _minColliderSize;
                break;
            case PlaneDirection.YPos:
            case PlaneDirection.YNeg:
                _quadMesh.Size = new Vector2(_volume.Size.X, _volume.Size.Z);
                pos.Y += _volume.Size.X * (_planeDirection == PlaneDirection.YNeg ? -.5f : .5f);
                boxSize.Y = _minColliderSize;
                break;
            case PlaneDirection.ZPos:
            case PlaneDirection.ZNeg:
                _quadMesh.Size = new Vector2(_volume.Size.X, _volume.Size.Y);
                pos.Z += _volume.Size.Z * (_planeDirection == PlaneDirection.ZNeg ? -.5f : .5f);
                boxSize.Z = _minColliderSize;
                break;
        }

        _colliderBox.Size = boxSize;
        Position = pos;
    }
}