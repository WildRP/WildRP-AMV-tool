using Godot;
using System;

public partial class OrbitCamera : Control
{
    [Export] private float _scrollSpeed = 10;
    [Export] private float _orbitSpeed = 10;
    [Export] private float _defaultDistance = 20;
    [Export] private Camera3D _cameraNode;
    [Export] private SubViewport _viewport;
    [Export] private SpringArm3D _anchor;
    
    private Vector2 _moveSpeed;
    private Vector3 _rotation;
    
    public override void _Ready()
    {
        _anchor.SpringLength = _defaultDistance;

        _rotation = _anchor.Transform.Basis.GetRotationQuaternion().GetEuler();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;

        _rotation.X += -_moveSpeed.Y * dt * _orbitSpeed;
        _rotation.Y += -_moveSpeed.X * dt * _orbitSpeed;
        _rotation.X = Mathf.Clamp(_rotation.X, -(Mathf.Pi / 2), Mathf.Pi / 2);
        GD.Print(_rotation.X);
        _moveSpeed = Vector2.Zero;
        
        _anchor.SetIdentity();
        _anchor.Basis = new Basis(Quaternion.FromEuler(_rotation));
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton:
                Input.MouseMode = mouseButton.Pressed
                    ? Input.MouseModeEnum.Captured
                    : Input.MouseModeEnum.Visible;
                
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseMotion mouseMotion when Input.MouseMode == Input.MouseModeEnum.Captured:
                _moveSpeed = mouseMotion.Relative;
                break;
        }
    }
}
