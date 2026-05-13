using System;
using System.IO;
using GameLogic;
using GameLogic.Events;
using GameLogic.MyApp.Exceptions;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    ///     Manages the simulation runtime, acting as a bridge
    ///     between the Unity engine and the simulation library.
    /// </summary>
    public class SimController : MonoBehaviour
    {
        // === MODELS ===

        /// <summary>
        ///     Represents the sim to game world scale factor.
        /// </summary>
        public static readonly int WorldScale = 10;

        /// <summary>
        ///
        /// </summary>
        private uint SelectedId
        {
            get => _selectedId;
            set
            {
                _selectedId = value;
                OnSelectedUnitChanged?.Invoke(value);
            }
        }

        private uint _selectedId;

        private int _selectedXCoord;
        private int _selectedYCoord;

        private const UnitTeam ClientTeam = UnitTeam.Blue;

        /// <summary>
        ///     Simulation state.
        /// </summary>
        private GameState _simState;

        ///////////////////
        // UNITY DRIVERS //
        ///////////////////

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Maps", "map.json");
            string jsonString = File.ReadAllText(path);

            _simState = new GameState(jsonString);
            _simState.TurnStateMachine.Init();
            _simState.EventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChanged);
            _simState.EventBus.Subscribe<UnitDamagedEvent>(HandleUnitDamaged);
            _simState.EventBus.Subscribe<UnitMovedEvent>(HandleUnitMoved);
            _simState.EventBus.Subscribe<UnitSpentActionEvent>(HandleUnitSpentAction);
        }

        ///////////////////
        // STATE DRIVERS //
        ///////////////////

        /// <summary>
        ///     Raised when the selected unit changes.
        /// </summary>
        public event Action<uint> OnSelectedUnitChanged;

        /// <summary>
        ///     Raised when the turn state changes.
        /// </summary>
        public event Action<TurnStateChangeEvent> OnTurnStateChanged;

        /// <summary>
        ///     Raised when a unit is damaged.
        /// </summary>
        public event Action<UnitDamagedEvent> OnUnitDamaged;

        /// <summary>
        ///     Raised when a unit is moved.
        /// </summary>
        public event Action<UnitMovedEvent> OnUnitMoved;

        /// <summary>
        ///     Raised when a unit action point is changed.
        /// </summary>
        public event Action<UnitSpentActionEvent> OnActionSpent;

        /// <summary>
        ///     Raised when movement coordinates are to be highlighted.
        /// </summary>
        public event Action<(uint, uint)[]> OnHighlightMovement;

        /// <summary>
        ///     Raised when targets are to be highlighted.
        /// </summary>
        public event Action<UnitView[]> OnHighlightTargets;

        /// <summary>
        ///     Raised when a selected unit is to be highlighted.
        /// </summary>
        public event Action<(uint, uint)> OnHighlightSelection;

        /// <summary>
        ///     Raised on highlight reset.
        /// </summary>
        public event Action OnResetHighlight;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        public bool TrySelectUnitAt(int xCoord, int yCoord)
        {
            if (TurnState != TurnState.BlueTurn) return false;
            if (xCoord < 0 || xCoord >= _simState.MapX || yCoord < 0 || yCoord >= _simState.MapY) return false;
            uint id = _simState.Map[xCoord][yCoord].UnitId;

            // Deselect.
            if (id == 0)
            {
                SelectedId = 0;
                SetTileHighlights();
                return false;
            }

            if (!TryGetUnitById(id, out UnitView unit)) throw new ImpossibleStateException();
            if (unit.Team != ClientTeam) return false;

            SelectedId = id;
            _selectedXCoord = xCoord;
            _selectedYCoord = yCoord;
            Debug.Log($"SelectUnitAt: {SelectedId}");
            SetTileHighlights();

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        public bool TrySelectUnitAction(int xCoord, int yCoord)
        {
            if (SelectedId == 0) return false;
            return _simState.Map[xCoord][yCoord].UnitId != 0
                ? TryAttackWithSelectedUnit(xCoord, yCoord)
                : TryMoveSelectedUnit(xCoord, yCoord);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        private bool TryMoveSelectedUnit(int xCoord, int yCoord)
        {
            if (SelectedId == 0) return false;
            if (_simState.Map[xCoord][yCoord].UnitId != 0) return false;
            if (!TryGetUnitById(SelectedId, out UnitView unit)) throw new ImpossibleStateException();
            // Dijkstra is slow, in future, an exact path should be passed in which can be checked.
            if (Array.IndexOf(GetMoveableCoords(unit), ((uint)xCoord, (uint)yCoord)) == -1) return false;

            try
            {
                MoveUnit(unit, xCoord, yCoord);
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentOutOfRangeException)
            {
                Debug.LogError($"Failed to move unit {unit.Id}: {e}");
                return false;
            }

            Debug.Log(
                $"Moved selected unit {SelectedId} from" +
                $" {_selectedXCoord},{_selectedYCoord} -> {xCoord},{yCoord}");
            _selectedXCoord = xCoord;
            _selectedYCoord = yCoord;
            SetTileHighlights();

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void MoveUnit(UnitView unit, int xCoord, int yCoord)
        {
            _simState.ActionMoveUnit(ViewToPtr(unit), (uint)xCoord, (uint)yCoord);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        private bool TryAttackWithSelectedUnit(int xCoord, int yCoord)
        {
            if (SelectedId == 0) return false;
            if (_simState.Map[xCoord][yCoord].UnitId == 0) return false;
            if (!TryGetUnitById(SelectedId, out UnitView unit) ||
                !TryGetUnitById(_simState.Map[xCoord][yCoord].UnitId, out UnitView target))
                throw new ImpossibleStateException();
            foreach (UnitView v in GetAttackableUnits(unit))
                if (v.Id == target.Id) break;
                else return false;

            try
            {
                AttackWithUnit(unit, target);
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentOutOfRangeException)
            {
                Debug.LogError($"Failed to attack unit {target.Id} with unit {unit.Id}: {e}");
                return false;
            }

            Debug.Log($"Attacked unit {target.Id} with {unit.Id}");

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="target"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AttackWithUnit(UnitView unit, UnitView target)
        {
            _simState.ActionAttackUnit(ViewToPtr(unit), ViewToPtr(target));
        }


        /// <summary>
        ///     Highlight required tiles based on unit selection.
        /// </summary>
        private void SetTileHighlights()
        {
            OnResetHighlight?.Invoke();

            if (SelectedId == 0) return;

            if (!TryGetUnitById(SelectedId, out UnitView unit)) throw new ImpossibleStateException();

            OnHighlightSelection?.Invoke((unit.X, unit.Y));
            if (unit.Actions == 0) return;

            (uint, uint)[] moveableCoords = GetMoveableCoords(unit);
            if (moveableCoords.Length > 0) OnHighlightMovement?.Invoke(moveableCoords);

            UnitView[] targets = GetAttackableUnits(unit);
            if (targets.Length > 0) OnHighlightTargets?.Invoke(targets);
        }

        /// <summary>
        /// </summary>
        public void EndTurn()
        {
            SelectedId = 0;
            _simState.TurnStateMachine.EndTurn();
        }

        public void TestVictory()
        {
            _simState.TurnStateMachine.BlueVictory();
        }


        // /// <summary>
        // ///
        // /// </summary>
        // /// <param name="xCoord"></param>
        // /// <param name="yCoord"></param>
        // public bool EnemySelectUnitAt(int xCoord, int yCoord)
        // {
        //     uint id = _simState.Map[xCoord][yCoord].UnitId;
        //     if (id == 0)
        //     {
        //         SelectedId = 0;
        //         return false;
        //     }
        //
        //     if (!_simState.TryGetUnit(id, out Unit unit)) throw new ImpossibleStateException();
        //
        //     _selectedId = id;
        //     _selectedXCoord = xCoord;
        //     _selectedYCoord = yCoord;
        //     Debug.Log($"SelectUnitAt: {SelectedId}");
        //     return true;
        // }

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


        ////////////////////
        // STATE QUERIES //
        ///////////////////

        /// <summary>
        ///     Get the map layout.
        /// </summary>
        public Tile[][] Map => _simState.Map;

        /// <summary>
        ///     Get map width.
        /// </summary>
        public uint MapX => _simState.MapX;

        /// <summary>
        ///     Get map height.
        /// </summary>
        public uint MapY => _simState.MapY;

        /// <summary>
        ///     Get turn state.
        /// </summary>
        public TurnState TurnState => _simState.TurnStateMachine.State;

        /// <summary>
        ///
        /// </summary>
        /// <param name="unitId"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool TryGetUnitById(uint unitId, out UnitView unit)
        {
            if (!_simState.TryGetUnit(unitId, out Unit u))
            {
                unit = default;
                return false;
            }

            if (!_simState.TryGetUnitCoords(u.Id, out (uint X, uint Y) coords))
                throw new ImpossibleStateException();
            unit = new UnitView(u, coords.X, coords.Y);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public UnitView[] GetUnitsByTeam(UnitTeam team)
        {
            Unit[] units = _simState.GetUnitsByTeam(team);
            var views = new UnitView[units.Length];
            for (var i = 0; i < units.Length; i++)
            {
                if (!_simState.TryGetUnitCoords(units[i].Id, out (uint X, uint Y) coords))
                    throw new ImpossibleStateException();
                views[i] = new UnitView(units[i], coords.X, coords.Y);
            }

            return views;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public (uint, uint)[] GetMoveableCoords(UnitView unit) =>
            _simState.GetMoveableCoords(unit.X, unit.Y, unit.Type);

        /// <summary>
        ///
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public UnitView[] GetAttackableUnits(UnitView unit)
        {
            Unit[] units = _simState.GetAttackableUnitsFromCoord(unit.X, unit.Y, unit.Type, unit.Team);
            var views = new UnitView[units.Length];
            for (var i = 0; i < units.Length; i++)
            {
                if (!_simState.TryGetUnitCoords(units[i].Id, out (uint X, uint Y) coords))
                    throw new ImpossibleStateException();
                views[i] = new UnitView(units[i], coords.X, coords.Y);
            }

            return views;
        }

        private Unit ViewToPtr(UnitView unit)
            => !_simState.TryGetUnit(unit.Id, out Unit u)
                ? throw new ImpossibleStateException()
                : u;

        ////////////////////
        // EVENT HANDLING //
        ////////////////////

        /// <summary>
        ///     Forwards raised sim <see cref="TurnStateChangeEvent" />.
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleTurnStateChanged(TurnStateChangeEvent simEvent)
        {
            Debug.Log($"[TurnStateChanged] Turn {simEvent.TurnCounter + 1} {simEvent.OldState} -> {simEvent.NewState}");
            OnTurnStateChanged?.Invoke(simEvent);

            // Mock red turn.
            // if (simEvent.NewState == TurnState.RedTurn)
            //     MockRedTurnStart();
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

        /// <summary>
        ///     Forwards raised sim <see cref="UnitMovedEvent" />.
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleUnitMoved(UnitMovedEvent simEvent)
        {
            Debug.Log(
                $"[UnitMoved] Unit {simEvent.UnitId} moved " +
                $"{simEvent.OldCoords.Item1},{simEvent.OldCoords.Item2}) -> " +
                $"({simEvent.NewCoords.Item1},{simEvent.NewCoords.Item2})"
            );
            OnUnitMoved?.Invoke(simEvent);
        }

        private void HandleUnitSpentAction(UnitSpentActionEvent simEvent)
        {
            Debug.Log(
                $"[UnitActionPoint] Unit {simEvent.UnitId} actions " +
                $"({simEvent.OldActions} -> {simEvent.NewActions})"
            );
            OnActionSpent?.Invoke(simEvent);
        }
    }
}