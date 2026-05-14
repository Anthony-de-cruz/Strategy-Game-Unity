using System.Collections;
using GameLogic;
using GameLogic.Events;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        public Image background;

        private SimController _simController;
        private uint _id;
        private UnitTeam _team;

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
            _simController.OnUnitDamaged += HandleUnitAttacked;
            _simController.OnActionSpent += HandleUnitSpentAction;
        }

        private void OnDisable()
        {
            if (!_simController) return;
            _simController.OnUnitDamaged -= HandleUnitAttacked;
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

        private void OnDestroy()
        {
            if (!_simController) return;
            _simController.OnUnitDamaged -= HandleUnitAttacked;
            _simController.OnActionSpent -= HandleUnitSpentAction;
        }

        /// <summary>
        ///
        /// </summary>
        public void Init(SimController sim, uint initId, UnitType initType, UnitTeam initTeam, uint initStrength)
        {
            _simController = sim;
            _id = initId;
            _team = initTeam;

            _simController.OnUnitDamaged += HandleUnitAttacked;
            _simController.OnActionSpent += HandleUnitSpentAction;

            type.text = initType.ToString().ToUpper();
            strength.text = $"STR: {initStrength}";
            actions.text = "ACTIONS: 2/2";
            background.color = initTeam == UnitTeam.Red
                ? new Color(0.8f, 0.1f, 0.1f, 0.50f)
                : new Color(0.1f, 0.25f, 0.9f, 0.50f);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        private void HandleUnitAttacked(UnitAttackedEvent e)
        {
            if (e.TargetId == _id)
            {
                if (e.NewStrength == 0)
                {
                    Destroy(this);
                }
                else
                {
                    strength.text = $"STR: {e.NewStrength}";
                }

                return;
            }

            if (e.AttackerId != _id) return;

            StartCoroutine(FlashRoutine());
            return;

            IEnumerator FlashRoutine()
            {
                background.color = Color.white;
                yield return new WaitForSeconds(0.25f);
                background.color = _team == UnitTeam.Red
                    ? new Color(0.8f, 0.1f, 0.1f, 0.50f)
                    : new Color(0.1f, 0.25f, 0.9f, 0.50f);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        private void HandleUnitSpentAction(UnitSpentActionEvent e)
        {
            if (e.UnitId != _id) return;
            actions.text = $"ACTIONS: {e.NewActions}/2";
        }
    }
}