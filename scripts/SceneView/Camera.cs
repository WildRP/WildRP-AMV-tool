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
    
    private Vector2 _currentCamOrbitSpeed;
    private Vector3 _cameraRotation;
    private bool _rotatingCamera;
    
    void SetupCamera()
    {
        _anchor.SpringLength = _defaultCamDistance;
        _cameraRotation = _anchor.Transform.Basis.GetRotationQuaternion().GetEuler();
    }

    void ProcessCamera(float delta)
    {

        _cameraRotation.X += -_currentCamOrbitSpeed.Y * delta * _cameraOrbitSpeed;
        _cameraRotation.Y += -_currentCamOrbitSpeed.X * delta * _cameraOrbitSpeed;
        _cameraRotation.X = Mathf.Clamp(_cameraRotation.X, -(Mathf.Pi / 2), Mathf.Pi / 2);
        _currentCamOrbitSpeed = Vector2.Zero;
        
        _anchor.SetIdentity();
        _anchor.Basis = new Basis(Quaternion.FromEuler(_cameraRotation));
    }

    void CameraInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton:
                Input.MouseMode = mouseButton.Pressed
                    ? Input.MouseModeEnum.Captured
                    : Input.MouseModeEnum.Visible;

                _rotatingCamera = Input.MouseMode == Input.MouseModeEnum.Captured;
                
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseMotion mouseMotion when _rotatingCamera:
                _currentCamOrbitSpeed = mouseMotion.Relative;
                break;
        }
    }
}