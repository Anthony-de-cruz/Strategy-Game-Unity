using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>
        /// </summary>
        public PlayerInput Input { get; private set; }

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            Input ??= new PlayerInput();
        }

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            Input ??= new PlayerInput(); // Guard clause for unity editor lazy loading of input system.
            Input.Enable();
        }

        /// <summary>
        ///     Called on game object disabled.
        /// </summary>
        private void OnDisable()
        {
            Input.Disable();
        }
    }
}