using System;
using System.Collections;
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

        public UnitTeam team = UnitTeam.Red;

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

        // /// <summary>
        // /// </summary>
        // private void MockRedTurnStart()
        // {
        //     StartCoroutine(MockRedTurnEndCoroutine());
        // }
        //
        // /// <summary>
        // /// </summary>
        // /// <returns></returns>
        // private IEnumerator MockRedTurnEndCoroutine()
        // {
        //     yield return new WaitForSeconds(1f);
        //
        //     EnemySelectUnitAt(13, 15 - _simState.TurnStateMachine.TurnCounter);
        //     TryMoveSelectedUnitTo(13, 14 - _simState.TurnStateMachine.TurnCounter);
        //
        //     yield return new WaitForSeconds(1f);
        //     _simState.TurnStateMachine.EndTurn();
        // }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        private void HandleTurnStateChange(TurnStateChangeEvent e)
        {
            if (e.NewState != TurnState.RedTurn) return;
            if (TryGetNextMove(out GameAction action))
            {
                switch (action)
                {
                    case MoveAction moveAction:
                        throw new NotImplementedException();
                    case AttackAction attackAction:
                        simController.AttackWithUnit(attackAction.Attacker, attackAction.Target);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }

                return;
            }
            simController.EndTurn();
        }

        // private IEnumerator PerformAction(GameAction action)
        // {
        //     yield return new WaitForSeconds(1f);
        //
        //     EnemySelectUnitAt(13, 15 - _simState.TurnStateMachine.TurnCounter);
        //     TryMoveSelectedUnitTo(13, 14 - _simState.TurnStateMachine.TurnCounter);
        //
        //     yield return new WaitForSeconds(1f);
        //     _simState.TurnStateMachine.EndTurn();
        //
        // }

        /// <summary>
        ///
        /// </summary>
        private bool TryGetNextMove(out GameAction action)
        {
            UnitView[] units = simController.GetUnitsByTeam(team);
            if (units.Length == 0)
            {
                action = null;
                return false;
            }

            foreach (UnitView unit in units)
            {
                while (unit.Actions > 0)
                {
                    UnitView[] attackableUnits = simController.GetAttackableUnits(unit);
                    if (attackableUnits.Length == 0) break;
                    action = new AttackAction(unit, attackableUnits[0]);
                    return true;
                }
            }

            action = null;
            return false;
        }
    }

    /// <summary>
    ///
    /// </summary>
    internal abstract record GameAction;

    /// <inheritdoc/>
    internal sealed record MoveAction(UnitView Unit, int X, int Y) : GameAction
    {
        public UnitView Unit { get; } = Unit;
        public int X { get; } = X;
        public int Y { get; } = Y;
    }

    /// <inheritdoc />
    internal sealed record AttackAction(UnitView Attacker, UnitView Target) : GameAction
    {
        public UnitView Attacker { get; } = Attacker;
        public UnitView Target { get; } = Target;
    }
}