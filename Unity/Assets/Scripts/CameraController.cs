using UnityEngine;

/// <summary>
/// 
/// </summary>
public class CameraController : MonoBehaviour
{
    public float moveSpeed = 25f;

    public float minY = 10f;
    public float maxY = 60f;

    public Vector2 xBounds = new(-100, 100);
    public Vector2 yBounds = new(-100, 100);
    public Vector2 zBounds = new(-100, 100);

    public float yawSpeed = 0.2f;
    public float pitchSpeed = 0.2f;

    public float minPitch = -20f;
    public float maxPitch = 120f;

    public float screenEdgeSize = 10f;

    PlayerInput _input;

    Vector3 _moveInput;
    Vector2 _lookInput;
    Vector2 _mousePos;
    bool _cameraRotating;
    float _pitch;
    float _yaw;

    /// <summary>
    /// Called on script load.
    /// </summary>
    void Awake()
    {
        _input = new PlayerInput();
    }

    /// <summary>
    /// Called on game object enabled.
    /// </summary>
    void OnEnable()
    {
        _input.Enable();

        _input.Player.CameraMove.performed += ctx => _moveInput = ctx.ReadValue<Vector3>();
        _input.Player.CameraMove.canceled += _ => _moveInput = Vector3.zero;

        _input.Player.CameraRotateToggle.performed += _ => _cameraRotating = true;
        _input.Player.CameraRotateToggle.canceled += _ => _cameraRotating = false;

        _input.Player.CameraRotate.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        _input.Player.CameraRotate.canceled += _ => _lookInput = Vector2.zero;
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
        HandleKeyboardPan();
        HandleRotation();
        //HandleEdgeScroll();
        ClampPosition();
    }

    /// <summary>
    /// 
    /// </summary>
    void HandleKeyboardPan()
    {
        Vector3 move = new(_moveInput.x, _moveInput.y, _moveInput.z);
        transform.Translate(moveSpeed * Time.deltaTime * move, Space.World);
    }

    /// <summary>
    /// 
    /// </summary>
    void HandleRotation()
    {
        if (!_cameraRotating) return;

        _yaw +=  _lookInput.x * yawSpeed;
        _pitch -= _lookInput.y * pitchSpeed;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        Debug.Log($"ROTATING: yaw: {_yaw} pitch: {_pitch} {_lookInput}");

        transform.rotation = Quaternion.Euler(-_pitch, _yaw, 0f);
    }

    /// <summary>
    /// 
    /// </summary>
    void HandleEdgeScroll()
    {
        Vector3 move = Vector3.zero;

        if (_mousePos.x < screenEdgeSize)
            move.x -= 1;

        if (_mousePos.x > Screen.width - screenEdgeSize)
            move.x += 1;

        if (_mousePos.y < screenEdgeSize)
            move.z -= 1;

        if (_mousePos.y > Screen.height - screenEdgeSize)
            move.z += 1;

        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// 
    /// </summary>
    void ClampPosition()
    {
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, xBounds.x, xBounds.y);
        pos.y = Mathf.Clamp(pos.y, yBounds.x, yBounds.y);
        pos.z = Mathf.Clamp(pos.z, zBounds.x, zBounds.y);

        transform.position = pos;
    }
}
