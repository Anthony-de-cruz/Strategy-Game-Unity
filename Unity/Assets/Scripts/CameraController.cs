using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public InputManager inputManager;

        public float moveSpeed = 25f;
        public float moveFastSpeed = 50f;
        public float yawSpeed = 0.2f;
        public float pitchSpeed = 0.2f;

        public Vector2 xBounds = new(0, 100);
        public Vector2 yBounds = new(0, 100);
        public Vector2 zBounds = new(0, 100);
        public Vector2 pitchBounds = new(-25f, 75f);

        private bool _isMovingFast;
        private bool _isRotating;
        private Vector3 _moveInput;
        private float _pitch = 20; // Infer starting pitch from object in future.
        private Vector2 _rotationInput;
        private float _yaw;

        /// <summary>
        ///     Called once per frame.
        /// </summary>
        private void Update()
        {
            HandleRotation();
            HandleKeyboardPan();
        }

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            inputManager.Input.Player.CameraMoveFastToggle.performed += _ => _isMovingFast = true;
            inputManager.Input.Player.CameraMoveFastToggle.canceled += _ => _isMovingFast = false;

            inputManager.Input.Player.CameraMove.performed += ctx => _moveInput = ctx.ReadValue<Vector3>();
            inputManager.Input.Player.CameraMove.canceled += _ => _moveInput = Vector3.zero;

            inputManager.Input.Player.CameraRotateToggle.performed += _ => _isRotating = true;
            inputManager.Input.Player.CameraRotateToggle.canceled += _ => _isRotating = false;

            inputManager.Input.Player.CameraRotate.performed += ctx => _rotationInput = ctx.ReadValue<Vector2>();
            inputManager.Input.Player.CameraRotate.canceled += _ => _rotationInput = Vector2.zero;
        }

        /// <summary>
        ///     Rotate the camera based on input.
        /// </summary>
        private void HandleRotation()
        {
            if (!_isRotating) return;

            _yaw += _rotationInput.x * yawSpeed;
            _pitch = Mathf.Clamp(_pitch - _rotationInput.y * pitchSpeed, pitchBounds.x, pitchBounds.y);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>
        ///     Pan the camera relative to camera yaw based on input.
        /// </summary>
        private void HandleKeyboardPan()
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
                Vector3.up * _moveInput.y;

            Vector3 pos = transform.position +
                          (_isMovingFast ? moveFastSpeed : moveSpeed) *
                          Time.deltaTime * move;

            pos.x = Mathf.Clamp(pos.x, xBounds.x, xBounds.y);
            pos.y = Mathf.Clamp(pos.y, yBounds.x, yBounds.y);
            pos.z = Mathf.Clamp(pos.z, zBounds.x, zBounds.y);
            transform.position = pos;
        }
    }
}