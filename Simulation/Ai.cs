using Simulation.Events;

namespace Simulation
{
    public class Ai
    {
        /// <summary>
        ///
        /// </summary>
        private readonly SimState _simState;

        /// <summary>
        ///
        /// </summary>
        /// <param name="simState"></param>
        public Ai(SimState simState)
        {
            _simState = simState;
            _simState.EventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChange);
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
            Unit[] units = _simState.GetUnitsByTeam(UnitTeam.Red);
            if (units.Length == 0) return;

            foreach (Unit unit in units)
            {
                while (unit.Actions > 0)
                {
                    // if (!_simState.TryGetUnitCoords(unit.Id, out (uint X, uint Y) coords)) break;
                    // //Unit[] attackableUnits = _simState.GetAttackableUnitsFromCoord(coords.X, coords.Y, unit.Type, unit.Team);
                    // if (attackableUnits.Length == 0) break;
                    // _simState.ActionAttackUnit(unit, attackableUnits[0]);
                }
            }

        }
    }
}