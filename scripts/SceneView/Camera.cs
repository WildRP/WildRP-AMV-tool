using System;
using Godot;

namespace WildRP.AMVTool.Sceneview;

public partial class SceneView
{
    [ExportGroup("Camera")]
    [Export] private float _scrollSpeed = 10;
    [Export] private float _cameraOrbitSpeed = 10;
    [Export] private float _defaultCamDistance = 20;
    [Export] private Camera3D _cameraNode;
    [Export] private SpringArm3D _anchor;
    [Export] private PanelContainer _sceneViewPanel;
    
    private bool _canScroll = false;
    private Vector2 _currentCamOrbitInput;
    private Vector3 _cameraRotation;
    private bool _rotatingCamera;
    private float _scrollInput;
    
    void SetupCamera()
    {
        _anchor.SpringLength = _defaultCamDistance;
        _cameraRotation = _anchor.Transform.Basis.GetRotationQuaternion().GetEuler();

        _sceneViewPanel.MouseEntered += () => _canScroll = true;
        _sceneViewPanel.MouseExited += () => _canScroll = false;

    }

    void ProcessCamera(float delta)
    {

        _cameraRotation.X += -_currentCamOrbitInput.Y * delta * _cameraOrbitSpeed;
        _cameraRotation.Y += -_currentCamOrbitInput.X * delta * _cameraOrbitSpeed;
        _cameraRotation.X = Mathf.Clamp(_cameraRotation.X, -(Mathf.Pi / 2), Mathf.Pi / 2);
        _currentCamOrbitInput = Vector2.Zero;

        _anchor.SpringLength = Mathf.Clamp(_anchor.SpringLength + _scrollInput * delta * _scrollSpeed, 1, 50);
        _scrollInput = 0;
        
        _anchor.SetIdentity();
        _anchor.Basis = new Basis(Quaternion.FromEuler(_cameraRotation));
    }

    void CameraInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                if (mouseButton.ButtonIndex == MouseButton.Right)
                {
                    Input.MouseMode = mouseButton.Pressed
                        ? Input.MouseModeEnum.Captured
                        : Input.MouseModeEnum.Visible;

                    _rotatingCamera = Input.MouseMode == Input.MouseModeEnum.Captured;
                }

                if (mouseButton.ButtonIndex is MouseButton.WheelDown or MouseButton.WheelUp && _canScroll)
                    _scrollInput = mouseButton.ButtonIndex == MouseButton.WheelDown ? -1 : 1;
                
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseMotion mouseMotion when _rotatingCamera:
                _currentCamOrbitInput = mouseMotion.Relative;
                GetViewport().SetInputAsHandled();
                break;
        }
    }
}