using UnityEngine;

/// <summary>
/// 
/// </summary>
public class CameraController : MonoBehaviour
{
    public float moveSpeed = 25f;
    public float moveFastSpeed = 50f;

    public float yawSpeed = 0.2f;
    public float pitchSpeed = 0.2f;

    public Vector2 xBounds = new(0, 100);
    public Vector2 yBounds = new(0, 100);
    public Vector2 zBounds = new(0, 100);

    public float minPitch = -25f;
    public float maxPitch = 75f;

    PlayerInput _input;

    bool _isMovingFast;
    Vector3 _moveInput;
    Vector2 _rotationInput;
    bool _isRotating;
    float _pitch;
    float _yaw;

    /// <summary>
    /// Called on script load.
    /// </summary>
    void Awake()
    {
        _input ??= new PlayerInput();
    }

    /// <summary>
    /// Called on game object enabled.
    /// </summary>
    void OnEnable()
    {
        _input ??= new PlayerInput(); // Guard clause for unity editor lazy loading of input system.
        _input.Enable();

        _input.Player.CameraMoveFastToggle.performed += _ => _isMovingFast = true;
        _input.Player.CameraMoveFastToggle.canceled += _ => _isMovingFast = false;

        _input.Player.CameraMove.performed += ctx => _moveInput = ctx.ReadValue<Vector3>();
        _input.Player.CameraMove.canceled += _ => _moveInput = Vector3.zero;

        _input.Player.CameraRotateToggle.performed += _ => _isRotating = true;
        _input.Player.CameraRotateToggle.canceled += _ => _isRotating = false;

        _input.Player.CameraRotate.performed += ctx => _rotationInput = ctx.ReadValue<Vector2>();
        _input.Player.CameraRotate.canceled += _ => _rotationInput = Vector2.zero;
    }

    /// <summary>
    /// Called on game object disabled.
    /// </summary>
    void OnDisable()
    {
        _input.Disable();
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        HandleRotation();
        HandleKeyboardPan();
    }

    /// <summary>
    /// Rotate the camera based on input.
    /// </summary>
    void HandleRotation()
    {
        if (!_isRotating) return;

        _yaw +=  _rotationInput.x * yawSpeed;
        _pitch = Mathf.Clamp(_pitch - _rotationInput.y * pitchSpeed, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    /// <summary>
    /// Pan the camera relative to camera yaw based on input.
    /// </summary>
    void HandleKeyboardPan()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        forward.y = 0f; // Remove pitch.
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move =
            right * _moveInput.x +
            forward * _moveInput.z +
            (Vector3.up * _moveInput.y);

        Vector3 pos = transform.position +
            (_isMovingFast ? moveFastSpeed : moveSpeed) *
            Time.deltaTime * move;

        pos.x = Mathf.Clamp(pos.x, xBounds.x, xBounds.y);
        pos.y = Mathf.Clamp(pos.y, yBounds.x, yBounds.y);
        pos.z = Mathf.Clamp(pos.z, zBounds.x, zBounds.y);
        transform.position = pos;
    }
}
