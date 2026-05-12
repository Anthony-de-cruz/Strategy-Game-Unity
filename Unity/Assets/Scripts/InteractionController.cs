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
        private bool _isSelectingUnitAction;
        
        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            inputManager.Input.Player.SelectUnit.performed += _ => _isSelectingUnit = true;
            inputManager.Input.Player.SelectUnitAction.performed += _ => _isSelectingUnitAction = true;
        }
        
        /// <summary>
        ///     Called once per frame.
        /// </summary>
        private void Update()
        {
            HandleSelectUnit();
            HandleSelectingUnitAction();
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

            int xCoord = (int)hit.point.x / SimController.WORLD_SCALE;
            int yCoord = (int)hit.point.z / SimController.WORLD_SCALE;
            Debug.Log($"Clicked world position: {hit.point} -> {xCoord},{yCoord}");
            simController.TrySelectUnitAt(xCoord, yCoord);
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleSelectingUnitAction()
        {
            if (!_isSelectingUnitAction) return;
            _isSelectingUnitAction = false;
            
            if (!Physics.Raycast(interactionRaycastCamera.ScreenPointToRay(
                    Mouse.current.position.ReadValue()), out RaycastHit hit))
                return;

            int xCoord = (int)hit.point.x / SimController.WORLD_SCALE;
            int yCoord = (int)hit.point.z / SimController.WORLD_SCALE;
            Debug.Log($"Clicked world position: {hit.point} -> {xCoord},{yCoord}");
            simController.TrySelectUnitAction(xCoord, yCoord);
        }
    }
}