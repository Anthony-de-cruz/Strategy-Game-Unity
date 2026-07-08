using System;
using System.Collections;
using System.IO;
using Simulation;
using Simulation.Events;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    ///     Manages the simulation runtime, acting as a bridge
    ///     between the Unity engine and the simulation library.
    /// </summary>
    public class SimController : MonoBehaviour
    {
        /// <summary>
        ///     Represents the sim to game world scale factor.
        /// </summary>
        public const int WorldScale = 10;

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
        private uint _selectedXCoord;
        private uint _selectedYCoord;

        private const UnitTeam ClientTeam = UnitTeam.Blue;

        /// <summary>
        ///     Simulation event bus. Can be shared between <see cref="_simState"/> instances.
        /// </summary>
        private readonly EventBus _eventBus = new();

        /// <summary>
        ///     Simulation state.
        /// </summary>
        private SimState _simState;

        private uint _currentMap;

        #region UNITY DRIVERS

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            _eventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChanged);
            _eventBus.Subscribe<UnitAttackedEvent>(HandleUnitAttacked);
            _eventBus.Subscribe<UnitMovedEvent>(HandleUnitMoved);
            _eventBus.Subscribe<UnitSpentActionEvent>(HandleUnitSpentAction);

            LoadSimState();
        }

        #endregion UNITY DRIVERS

        #region STATE DRIVERS

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
        public event Action<UnitAttackedEvent> OnUnitDamaged;

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
        ///     Raised on a complete state reset.
        /// </summary>
        public event Action OnStateReset;

        /// <summary>
        ///
        /// </summary>
        /// <param name="jsonString"></param>
        private void LoadSimState()
        {
            (
                string _,
                uint mapX,
                uint mapY,
                MapLoader.UnitData[] units
            ) = MapLoader.LoadMetaFromJson(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "TestMaps", "map0.json")));

            var terrainMapRaw = new Span<byte>(new byte[mapX * mapY * 4]);
            var heightMapRaw = new Span<byte>(new byte[mapX * mapY * 4]);

            if (File.OpenRead(Path.Combine(Application.streamingAssetsPath, "TestMaps", "map0terrain.raw"))
                    .Read(terrainMapRaw) != mapX * mapY * 4 ||
                File.OpenRead(Path.Combine(Application.streamingAssetsPath, "TestMaps", "map0height.raw"))
                    .Read(heightMapRaw) != mapX * mapY * 4)
                throw new InvalidConfigException();

            Tile[,] terrainMap = MapLoader.LoadTerrainMapFromRaw(terrainMapRaw, mapX, mapY);
            float[,] heightMap = MapLoader.LoadHeightMapFromRaw(heightMapRaw, mapX, mapY, 25);

            _simState?.Dispose();
            _simState = new SimState(_eventBus, terrainMap, heightMap, units);
            _simState.TurnStateMachine.Init();
        }

        public void ResetLevel()
        {
            // LoadSimState(_currentMap == 0
            //     ? _map0JsonString
            //     : _map1JsonString);
            LoadSimState();
            OnStateReset?.Invoke();
        }

        public void LoadLevel(uint number)
        {
            _currentMap = number;
            LoadSimState();
            // LoadSimState(_currentMap == 0
            //     ? _map0JsonString
            //     : _map1JsonString);
            OnStateReset?.Invoke();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        public bool TrySelectUnitAt(uint xCoord, uint yCoord)
        {
            if (TurnState != TurnState.BlueTurn) return false;
            if (xCoord >= _simState.MapWidth || yCoord >= _simState.MapHeight) return false;
            uint id = _simState.UnitMap[xCoord, yCoord];

            // Deselect.
            if (id == 0)
            {
                SelectedId = 0;
                SetTileHighlights();
                return false;
            }

            if (!TryGetUnitById(id, out UnitView unit)) throw new InvalidConfigException();
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
        public bool TrySelectUnitAction(uint xCoord, uint yCoord)
        {
            if (SelectedId == 0) return false;
            return _simState.UnitMap[xCoord, yCoord] != 0
                ? TryAttackWithSelectedUnit(xCoord, yCoord)
                : TryMoveSelectedUnit(xCoord, yCoord);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        private bool TryMoveSelectedUnit(uint xCoord, uint yCoord)
        {
            if (SelectedId == 0) return false;
            if (_simState.UnitMap[xCoord, yCoord] != 0) return false;
            if (!TryGetUnitById(SelectedId, out UnitView unit)) throw new InvalidConfigException();
            // Dijkstra is slow, in future, an exact path should be passed in which can be checked.
            if (Array.IndexOf(GetMoveableCoords(unit), (xCoord, yCoord)) == -1) return false;

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
        public void MoveUnit(UnitView unit, uint xCoord, uint yCoord)
        {
            OnResetHighlight?.Invoke();
            StartCoroutine(MoveRoutine());
            return;

            IEnumerator MoveRoutine()
            {
                _simState.TurnStateMachine.BeginAction();
                _simState.ActionMoveUnit(ViewToPtr(unit), xCoord, yCoord);
                _selectedXCoord = xCoord;
                _selectedYCoord = yCoord;
                yield return new WaitForSeconds(0.5f);
                _simState.TurnStateMachine.EndAction();
                SetTileHighlights();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        private bool TryAttackWithSelectedUnit(uint xCoord, uint yCoord)
        {
            if (SelectedId == 0) return false;
            if (_simState.UnitMap[xCoord, yCoord] == 0) return false;
            if (!TryGetUnitById(SelectedId, out UnitView unit) ||
                !TryGetUnitById(_simState.UnitMap[xCoord, yCoord], out UnitView target))
                throw new ImpossibleStateException();

            bool isAttackable = false;
            foreach (UnitView v in GetAttackableUnits(unit))
                if (v.Id == target.Id)
                {
                    isAttackable = true;
                    break;
                }

            if (!isAttackable) return false;

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
            OnResetHighlight?.Invoke();
            StartCoroutine(AttackRoutine());
            return;

            IEnumerator AttackRoutine()
            {
                _simState.TurnStateMachine.BeginAction();
                yield return new WaitForSeconds(0.25f);
                _simState.ActionAttackUnit(ViewToPtr(unit), ViewToPtr(target));
                yield return new WaitForSeconds(1f);
                _simState.TurnStateMachine.EndAction();
                SetTileHighlights();
            }
        }

        /// <summary>
        ///     Highlight required tiles based on unit selection.
        /// </summary>
        private void SetTileHighlights()
        {
            OnResetHighlight?.Invoke();

            if (SelectedId == 0) return;

            if (!TryGetUnitById(SelectedId, out UnitView unit)) throw new InvalidConfigException();

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

        #endregion STATE DRIVERS

        #region STATE QUERIES

        /// <inheritdoc cref="SimState.TerrainMap"/>
        public Tile[,] TerrainMap => _simState.TerrainMap;

        /// <inheritdoc cref="SimState.HeightMap"/>
        public float[,] HeightMap => _simState.HeightMap;

        /// <inheritdoc cref="SimState.MapWidth"/>
        public uint MapWidth => _simState.MapWidth;

        /// <inheritdoc cref="SimState.MapHeight"/>
        public uint MapHeight => _simState.MapHeight;

        /// <inheritdoc cref="TurnStateMachine.State"/>
        public TurnState TurnState => _simState.TurnStateMachine.State;

        /// <summary>
        ///
        /// </summary>
        /// <param name="unitId"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool TryGetUnitById(uint unitId, out UnitView unit)
        {
            if (!_simState.TryGetUnit(unitId, out Unit u))
            {
                unit = default;
                return false;
            }

            if (!_simState.TryGetUnitCoords(u.Id, out (uint X, uint Y) coords))
                throw new InvalidConfigException();
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
                    throw new InvalidConfigException();
                views[i] = new UnitView(units[i], coords.X, coords.Y);
            }

            return views;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
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
                    throw new InvalidConfigException();
                views[i] = new UnitView(units[i], coords.X, coords.Y);
            }

            return views;
        }

        private Unit ViewToPtr(UnitView unit)
            => !_simState.TryGetUnit(unit.Id, out Unit u)
                ? throw new InvalidConfigException()
                : u;

        #endregion STATE QUERIES

        #region EVENT HANDLING

        /// <summary>
        ///     Forwards raised sim <see cref="TurnStateChangeEvent" />.
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleTurnStateChanged(TurnStateChangeEvent simEvent)
        {
            Debug.Log($"[TurnStateChanged] Turn {simEvent.TurnCounter + 1} {simEvent.OldState} -> {simEvent.NewState}");
            OnTurnStateChanged?.Invoke(simEvent);

            switch (simEvent.NewState)
            {
                case TurnState.BlueVictory:
                case TurnState.RedVictory:
                    SelectedId = 0;
                    OnResetHighlight?.Invoke();
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        ///     Forwards raised sim <see cref="UnitAttackedEvent" />.
        /// </summary>
        /// <param name="simEvent"></param>
        private void HandleUnitAttacked(UnitAttackedEvent simEvent)
        {
            Debug.Log(
                $"[UnitAttacked] Unit {simEvent.TargetId} was attacked by Unit {simEvent.AttackerId} " +
                $"and lost {simEvent.OldStrength - simEvent.NewStrength} strength " +
                $"({simEvent.OldStrength} -> {simEvent.NewStrength})"
            );
            OnUnitDamaged?.Invoke(simEvent);

            if (simEvent.NewStrength == 0) SetTileHighlights();
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

        #endregion EVENT HANDLING
    }
}