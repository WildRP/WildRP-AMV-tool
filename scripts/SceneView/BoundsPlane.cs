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
    private bool _mouseHovering;
    private bool _dragging;
    private Vector3 _normal;

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
        
        _staticBody3D.MouseEntered += () => _mouseHovering = true;
        _staticBody3D.MouseExited += () => _mouseHovering = false;
        
        UpdatePlane();
        _normal = Position.Normalized();
    }

    public override void _Process(double delta)
    {
        if (_volume.Selected)
            Transparency = _mouseHovering ? 0f : 0.5f;
        else
            Transparency = .9f;

        _staticBody3D.InputRayPickable = _volume.Selected;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
        {
            _dragging = btn.Pressed && _mouseHovering && SceneView.RotatingCamera == false;
        }
        
        if (@event is InputEventMouseMotion motion && _dragging)
        {
            // TODO: This is very much a first implementation. Drag directions become inverted if the bounds rotate too far.
            // Would happily accept a pull request to make this work consistently.
            var mouseMotion = motion.Relative / GetViewport().GetWindow().Size;
            mouseMotion *= 8;
            
            bool positive = _planeDirection is PlaneDirection.XPos or PlaneDirection.YPos or PlaneDirection.ZPos;
            var move = 0f;

            var camGlobalRight = GetViewport().GetCamera3D().GlobalBasis.X;
            
            if (_planeDirection is PlaneDirection.YNeg or PlaneDirection.YPos)
            {
                move = -mouseMotion.Y;
            }
            else
            {
                move = -mouseMotion.X;
                move *= Mathf.Sign(_normal.Dot(camGlobalRight));
                if (positive) move *= -1;
            }
            
            _volume.ChangeSizeWithGizmo(_normal * move, positive);
        }
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
                pos.Y += _volume.Size.Y * (_planeDirection == PlaneDirection.YNeg ? -.5f : .5f);
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