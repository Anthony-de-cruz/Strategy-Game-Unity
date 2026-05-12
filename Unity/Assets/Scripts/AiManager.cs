using GameLogic;
using GameLogic.Events;
using UnityEngine;

namespace Assets.Scripts
{
    public class AiManager : MonoBehaviour
    {
        /// <summary>
        ///     The simulation state for the AI to drive.
        /// </summary>
        public SimController simController;
        public UnitTeam team;

        /// <summary>
        ///
        /// </summary>
        private void OnEnable()
        {
            simController.OnTurnStateChanged += HandleTurnStateChange;
        }

        /// <summary>
        ///
        /// </summary>
        private void OnDisable()
        {
            simController.OnTurnStateChanged -= HandleTurnStateChange;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        private void HandleTurnStateChange(TurnStateChangeEvent e)
        {
            if (e.NewState == TurnState.RedTurn) PerformTurn();
        }

        /// <summary>
        ///
        /// </summary>
        private void PerformTurn()
        {
            // Unit[] units = _gameState.GetUnitsByTeam(this.UnitTeam.Red);
            // if (units.Length == 0) return;
            //
            // foreach (Unit unit in units)
            // {
            //     while (unit.CurrentActions > 0)
            //     {
            //         if (!_gameState.TryGetUnitCoords(unit.Id, out (uint X, uint Y) coords)) break;
            //         Unit[] attackableUnits =
            //             _gameState.GetAttackableUnitsFromCoord(coords.X, coords.Y, unit.Type, unit.Team);
            //         if (attackableUnits.Length == 0) break;
            //         _gameState.ActionAttackUnit(unit, attackableUnits[0]);
            //     }
            // }
        }
    }
}
