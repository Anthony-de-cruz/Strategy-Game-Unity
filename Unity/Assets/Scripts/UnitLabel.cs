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

        private SimController simController;
        private uint id;

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
            if (simController)
                simController.OnUnitDamaged += HandleUnitDamaged;
        }

        private void OnDisable()
        {
            if (simController)
                simController.OnUnitDamaged -= HandleUnitDamaged;
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
            simController = sim;
            id = initId;

            simController.OnUnitDamaged += HandleUnitDamaged;

            type.text = initType.ToString().ToUpper();
            strength.text = $"STR: {initStrength}";
        }

        private void HandleUnitDamaged(UnitDamagedEvent damagedEvent)
        {
            if (damagedEvent.UnitId != id)
                return;

            if (damagedEvent.NewStrength <= 0)
                Destroy(this);

            // Update text.
        }
    }
}