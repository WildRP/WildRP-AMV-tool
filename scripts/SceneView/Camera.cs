using System.Linq;
using Godot;

namespace WildRP.AMVTool.Sceneview;

// This should really be a script on the Camera but I made some weird decisions when I started this project
public partial class SceneView
{
    [ExportGroup("Camera")]
    [Export] private float _scrollSpeed = 10;
    [Export] private float _cameraOrbitSpeed = 10;
    [Export] private float _defaultCamDistance = 20;
    [Export] private Camera3D _cameraNode;
    [Export] private SpringArm3D _anchor;
    
    private static bool _canScrollOrPan;
    private Vector2 _currentCamOrbitInput;
    private Vector2 _currentCamPanInput;
    private Vector3 _cameraRotation;

    private bool _middleMouseDown, _spacebarDown;
    private bool PanningCamera => _middleMouseDown || _spacebarDown;
    public static bool RotatingCamera { get; private set; }
    private float _scrollInput;

    private void SetupCamera()
    {
        _anchor.SpringLength = _defaultCamDistance;
        _cameraRotation = _anchor.Transform.Basis.GetRotationQuaternion().GetEuler();
    }
    
    private void ProcessCamera(float delta)
    {
        _canScrollOrPan = _sceneViewPanels.Any(t => t.MouseOver) && Visible;

        if (Visible)
        {
            _cameraRotation.X += -_currentCamOrbitInput.Y * delta * _cameraOrbitSpeed;
            _cameraRotation.Y += -_currentCamOrbitInput.X * delta * _cameraOrbitSpeed;
            _cameraRotation.X = Mathf.Clamp(_cameraRotation.X, -(Mathf.Pi / 2), Mathf.Pi / 2);
        }
        _currentCamOrbitInput = Vector2.Zero;

        _anchor.SpringLength = Mathf.Clamp(_anchor.SpringLength + _scrollInput * delta * _scrollSpeed, 1, 50);
        _scrollInput = 0;

        _anchor.Basis = Basis.Identity;
        _anchor.Basis = new Basis(Quaternion.FromEuler(_cameraRotation));

        if (PanningCamera && !RotatingCamera && _canScrollOrPan)
        {
            _anchor.GlobalPosition += _cameraNode.GlobalBasis * new Vector3(-_currentCamPanInput.X, _currentCamPanInput.Y, 0) * delta;
        }

        _currentCamPanInput = Vector2.Zero;
    }

    private void CameraInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                if (mouseButton.ButtonIndex == MouseButton.Right)
                {
                    Input.MouseMode = mouseButton.Pressed
                        ? Input.MouseModeEnum.Captured
                        : Input.MouseModeEnum.Visible;

                    RotatingCamera = Input.MouseMode == Input.MouseModeEnum.Captured;
                }

                if (mouseButton.ButtonIndex is MouseButton.WheelDown or MouseButton.WheelUp && (_canScrollOrPan || AmvBaker.Instance.BakeInProgress))
                    _scrollInput = mouseButton.ButtonIndex == MouseButton.WheelDown ? 1 : -1;

                if (mouseButton.ButtonIndex is MouseButton.Middle)
                    _middleMouseDown = mouseButton.Pressed;
                
                GetViewport().SetInputAsHandled();
                break;
            
            case InputEventKey keyboardKey:
                if (keyboardKey.Keycode == Key.Space)
                    _spacebarDown = keyboardKey.Pressed;

                if (keyboardKey.Keycode == Key.F)
                    _anchor.GlobalPosition = Vector3.Zero;
                
                break;
            
            case InputEventMouseMotion mouseMotion when RotatingCamera:
                _currentCamOrbitInput = mouseMotion.Relative;
                GetViewport().SetInputAsHandled();
                break;
            
            case InputEventMouseMotion mouseMotion when PanningCamera:
                _currentCamPanInput = mouseMotion.Relative;
                GetViewport().SetInputAsHandled();
                break;
        }
    }
}