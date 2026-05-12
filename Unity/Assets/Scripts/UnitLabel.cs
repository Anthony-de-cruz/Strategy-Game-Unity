using GameLogic;
using GameLogic.Events;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    /// <summary>
    ///
    /// </summary>
    public class UnitLabel : MonoBehaviour
    {
        public Camera targetCamera;
        public TMP_Text type;
        public TMP_Text actions;
        public TMP_Text strength;

        private SimController _simController;
        private uint _id;

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            if (!targetCamera)
                targetCamera = Camera.main;
        }

        /// <summary>
        ///
        /// </summary>
        private void OnEnable()
        {
            if (!_simController) return;
            _simController.OnUnitDamaged += HandleUnitDamaged;
            _simController.OnActionSpent += HandleUnitSpentAction;
        }

        private void OnDisable()
        {
            if (!_simController) return;
            _simController.OnUnitDamaged -= HandleUnitDamaged;
            _simController.OnActionSpent -= HandleUnitSpentAction;
        }

        /// <summary>
        ///
        /// </summary>
        private void LateUpdate()
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - targetCamera.transform.position
            );
        }

        /// <summary>
        ///
        /// </summary>
        public void Init(SimController sim, uint initId, UnitType initType, UnitTeam initTeam, uint initStrength)
        {
            _simController = sim;
            _id = initId;

            _simController.OnUnitDamaged += HandleUnitDamaged;
            _simController.OnActionSpent += HandleUnitSpentAction;

            type.text = initType.ToString().ToUpper();
            strength.text = $"STR: {initStrength}";
            actions.text = "ACTIONS: 2/2";
        }

        private void HandleUnitDamaged(UnitDamagedEvent e)
        {
            if (e.UnitId != _id) return;
            if (e.NewStrength == 0) Destroy(this);

            strength.text = $"STR: {e.NewStrength}";
        }

        private void HandleUnitSpentAction(UnitSpentActionEvent e)
        {
            if (e.UnitId != _id) return;
            actions.text = $"ACTIONS: {e.NewActions}/2";
        }
    }
}