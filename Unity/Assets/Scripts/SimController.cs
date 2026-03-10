using System;
using System.Collections;
using GameLogic;
using GameLogic.Events;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    ///     Manages the simulation runtime, acting as a bridge
    ///     between the Unity engine and the simulation library.
    /// </summary>
    public class SimController : MonoBehaviour
    {
        /// === MODELS ===
        /// <summary>
        ///     Represents the sim to game world scale factor.
        /// </summary>
        public const int WORLD_SCALE = 10;

        public GameObject prefabInfantryBlue;
        public GameObject prefabInfantryRed;
        public GameObject prefabTankBlue;
        public GameObject prefabTankRed;

        /// <summary>
        ///     Simulation state.
        /// </summary>
        private GameState _simState;

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            _simState = new GameState(50, 50);
            _simState.TurnStateMachine.Init();
            _simState.EventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChanged);
            _simState.EventBus.Subscribe<UnitDamagedEvent>(HandleUnitDamaged);

            for (var i = 0; i < 3; i++) CreateUnit(UnitTeam.Blue, UnitType.Infantry, i + 12, 10);
            for (var i = 0; i < 2; i++) CreateUnit(UnitTeam.Blue, UnitType.Tank, i + 10, 9);
            for (var i = 0; i < 3; i++) CreateUnit(UnitTeam.Red, UnitType.Tank, i + 11, 15);

            _simState.TryGetUnit(1, out Unit unit);

            unit.Strength -= 2;
        }

        /// === SIM ===
        /// <summary>
        ///     Raised when the turn state changes.
        /// </summary>
        public event Action<TurnStateChangeEvent> OnTurnStateChanged;

        /// <summary>
        ///     Raised when a unit is damaged.
        /// </summary>
        public event Action<UnitDamagedEvent> OnUnitDamaged;

        /// <summary>
        ///
        /// </summary>
        public void EndTurn()
        {
            _simState.TurnStateMachine.EndTurn();
        }

        /// <summary>
        /// </summary>
        /// <param name="team"></param>
        /// <param name="type"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        private void CreateUnit(UnitTeam team, UnitType type, int xCoord, int yCoord)
        {
            Unit newUnit = _simState.CreateUnit(team, type, xCoord, yCoord);

            GameObject prefab = (type, team) switch
            {
                (UnitType.Infantry, UnitTeam.Blue) => prefabInfantryBlue,
                (UnitType.Infantry, UnitTeam.Red) => prefabInfantryRed,
                (UnitType.Tank, UnitTeam.Blue) => prefabTankBlue,
                (UnitType.Tank, UnitTeam.Red) => prefabTankRed,
                _ => throw new NotImplementedException()
            };

            Quaternion rotation = team switch
            {
                UnitTeam.Blue => Quaternion.Euler(0f, 0f, 0f),
                UnitTeam.Red => Quaternion.Euler(0f, 180f, 0f),
                _ => throw new NotImplementedException()
            };

            GameObject obj = Instantiate(prefab, new Vector3(xCoord * WORLD_SCALE, 0.5f, yCoord * WORLD_SCALE),
                rotation);
            Debug.Log(
                $"Unit {newUnit.Id} of type {newUnit.Type} instantiated" +
                $" @ {xCoord},{yCoord}/{obj.transform.position}");
        }

        /// <summary>
        /// </summary>
        private void MockRedTurnStart()
        {
            StartCoroutine(MockRedTurnEndCoroutine());
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private IEnumerator MockRedTurnEndCoroutine()
        {
            yield return new WaitForSeconds(2f);
            _simState.TurnStateMachine.EndTurn();
        }

        /// <summary>
        ///     Forwards raised sim <see cref="TurnStateChangeEvent" />.
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleTurnStateChanged(TurnStateChangeEvent simEvent)
        {
            Debug.Log($"[TurnStateChanged] Turn {simEvent.TurnCounter + 1} {simEvent.OldState} -> {simEvent.NewState}");
            OnTurnStateChanged?.Invoke(simEvent);

            // Mock red turn.
            if (simEvent.NewState == TurnState.RedTurn)
                MockRedTurnStart();
        }

        /// <summary>
        ///     Forwards raised sim <see cref="UnitDamagedEvent" />.
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleUnitDamaged(UnitDamagedEvent simEvent)
        {
            Debug.Log(
                $"[UnitDamaged] Unit {simEvent.UnitId} lost " +
                $"{simEvent.OldStrength - simEvent.NewStrength} strength " +
                $"({simEvent.OldStrength} -> {simEvent.NewStrength})"
            );
            OnUnitDamaged?.Invoke(simEvent);
        }
    }
}