using System.Linq;
using GameLogic.Events;

namespace GameLogic
{
    public class Ai
    {
        /// <summary>
        ///
        /// </summary>
        private readonly GameState _gameState;

        /// <summary>
        ///
        /// </summary>
        /// <param name="gameState"></param>
        public Ai(GameState gameState)
        {
            _gameState = gameState;
            _gameState.EventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChange);
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
            Unit[] units = _gameState.GetUnitsByTeam(UnitTeam.Red);
            if (units.Length == 0) return;

            foreach (Unit unit in units)
            {
                while (unit.CurrentActions > 0)
                {
                    if (!_gameState.TryGetUnitCoords(unit.Id, out (uint X, uint Y) coords)) break;
                    Unit[] attackableUnits = _gameState.GetAttackableUnitsFromCoord(coords.X, coords.Y, unit.Type, unit.Team);
                    if (attackableUnits.Length == 0) break;
                    _gameState.ActionAttackUnit(unit, attackableUnits[0]);
                }
            }

        }
    }
}