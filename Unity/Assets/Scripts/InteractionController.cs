using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts
{
    /// <summary>
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        public InputManager inputManager;
        public Camera interactionRaycastCamera;
        
        public SimController simController;

        private bool _isSelectingUnit;
        private bool _isSelectingMovePosition;
        
        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            inputManager.Input.Player.SelectUnit.performed += _ => _isSelectingUnit = true;
            inputManager.Input.Player.MoveSelectedUnit.performed += _ => _isSelectingMovePosition = true;
        }
        
        /// <summary>
        ///     Called once per frame.
        /// </summary>
        private void Update()
        {
            HandleSelectUnit();
            HandleSelectingMovePosition();
        }

        /// <summary>
        /// </summary>
        private void HandleSelectUnit()
        {
            if (!_isSelectingUnit) return;
            _isSelectingUnit = false;

            if (!Physics.Raycast(interactionRaycastCamera.ScreenPointToRay(
                    Mouse.current.position.ReadValue()), out RaycastHit hit))
                return;

            var xCoord = (int)hit.point.x / SimController.WORLD_SCALE;
            var yCoord = (int)hit.point.z / SimController.WORLD_SCALE;
            Debug.Log($"Clicked world position: {hit.point} -> {xCoord},{yCoord}");
            simController.SelectUnitAt(xCoord, yCoord);
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleSelectingMovePosition()
        {
            if (!_isSelectingMovePosition) return;
            _isSelectingMovePosition = false;
            
            if (!Physics.Raycast(interactionRaycastCamera.ScreenPointToRay(
                    Mouse.current.position.ReadValue()), out RaycastHit hit))
                return;
            Debug.Log($"Clicked world position: {hit.point}");
            
            var xCoord = (int)hit.point.x / SimController.WORLD_SCALE;
            var yCoord = (int)hit.point.z / SimController.WORLD_SCALE;
            simController.MoveSelectedUnitTo(xCoord, yCoord);
        }
    }
}