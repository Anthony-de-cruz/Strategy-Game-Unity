using System;
using System.Collections;
using System.Collections.Generic;
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
        public static readonly int WORLD_SCALE = 10;

        public GameObject prefabInfantryBlue;
        public GameObject prefabInfantryRed;
        public GameObject prefabTankBlue;
        public GameObject prefabTankRed;
        public GameObject prefabUnitLabel;

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

        private readonly Dictionary<uint, GameObject> _unitObjects = new();

        private UnitTeam _clientTeam = UnitTeam.Blue;

        /// <summary>
        ///     Simulation state.
        /// </summary>
        private GameState _simState;

        /// <summary>
        /// </summary>
        public TurnState TurnState => _simState.TurnStateMachine.State;

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            // Todo - Replace with relative path.
            var reader =
                new StreamReader(
                    File.OpenRead(@"C:\Users\Anthony\University\Game-Development\Strategy-Game-Unity\map.json"));
            string jsonString = reader.ReadToEnd();
            reader.Close();

            _simState = new GameState(jsonString);
            _simState.TurnStateMachine.Init();
            _simState.EventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChanged);
            _simState.EventBus.Subscribe<UnitDamagedEvent>(HandleUnitDamaged);
            _simState.EventBus.Subscribe<UnitSpentActionEvent>(HandleUnitSpentAction);

            for (var i = 0; i < 3; i++) CreateUnit(UnitTeam.Blue, UnitType.Infantry, i + 12, 10);
            for (var i = 0; i < 2; i++) CreateUnit(UnitTeam.Blue, UnitType.Tank, i + 10, 9);
            for (var i = 0; i < 3; i++) CreateUnit(UnitTeam.Red, UnitType.Tank, i + 11, 15);

            _simState.TryGetUnit(1, out Unit unit);

            unit.Strength -= 2;
        }

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
        public event Action<(uint, uint)[]> OnHighlightTargets;

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
            if (id == 0)
            {
                SelectedId = 0;
                OnResetHighlight?.Invoke();
                return false;
            }

            if (!_simState.TryGetUnit(id, out Unit unit)) throw new ImpossibleStateException();
            if (unit.Team != _clientTeam) return false;

            SelectedId = id;
            _selectedXCoord = xCoord;
            _selectedYCoord = yCoord;
            Debug.Log($"SelectUnitAt: {SelectedId}");

            OnResetHighlight?.Invoke();
            OnHighlightSelection?.Invoke(((uint)_selectedXCoord, (uint)_selectedYCoord));

            if (unit.CurrentActions <= 0) return true;

            // Todo - Clean up API since this is potentially obnoxious.
            (uint, uint)[] moveableCoords = _simState.GetMoveableCoords((uint)xCoord, (uint)yCoord, unit.Type);
            Unit[] targets = _simState.GetAttackableUnitsFromCoord((uint)xCoord, (uint)yCoord, unit.Type, unit.Team);
            var targetCoords = new (uint, uint)[targets.Length];
            for (var i = 0; i < targets.Length; i++)
            {
                _simState.TryGetUnitCoords(targets[i].Id, out (uint x, uint y) coords);
                targetCoords[i] = (coords.x, coords.y);
            }

            OnHighlightMovement?.Invoke(moveableCoords);
            OnHighlightTargets?.Invoke(targetCoords);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        public bool EnemySelectUnitAt(int xCoord, int yCoord)
        {
            uint id = _simState.Map[xCoord][yCoord].UnitId;
            if (id == 0)
            {
                SelectedId = 0;
                return false;
            }

            if (!_simState.TryGetUnit(id, out Unit unit)) throw new ImpossibleStateException();

            _selectedId = id;
            _selectedXCoord = xCoord;
            _selectedYCoord = yCoord;
            Debug.Log($"SelectUnitAt: {SelectedId}");
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        public bool TryMoveSelectedUnitTo(int xCoord, int yCoord)
        {
            if (SelectedId == 0) return false;
            if (_simState.Map[xCoord][yCoord].UnitId != 0) return false;
            if (!_simState.TryGetUnit(SelectedId, out Unit unit) ||
                !_unitObjects.TryGetValue(SelectedId, out GameObject unitObj))
                throw new ImpossibleStateException();
            if (_simState.Map[xCoord][yCoord].Type == TileType.Building && unit.Type == UnitType.Tank) return false;

            // _simState.Map[_selectedXCoord][_selectedYCoord].UnitId = 0;
            // _simState.Map[xCoord][yCoord].UnitId = SelectedId;
            _simState.ActionMoveUnit(unit, (uint)xCoord, (uint)yCoord);

            unitObj.transform.position = new Vector3(xCoord * WORLD_SCALE + 4, 0.5f, yCoord * WORLD_SCALE + 4);

            Debug.Log(
                $"Moved selected unit {SelectedId} from" +
                $" {_selectedXCoord},{_selectedYCoord} -> {xCoord},{yCoord}");
            _selectedXCoord = xCoord;
            _selectedYCoord = yCoord;
            return true;
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

            GameObject obj = Instantiate(prefab, new Vector3(xCoord * WORLD_SCALE + 4, 0.5f, yCoord * WORLD_SCALE + 4),
                rotation);
            _unitObjects.Add(newUnit.Id, obj);

            GameObject labelObj = Instantiate(prefabUnitLabel, obj.transform);
            labelObj.transform.localPosition = new Vector3(0f, 10f, 0f);
            var label = labelObj.GetComponent<UnitLabel>();
            label.Init(this, newUnit.Id, newUnit.Type, newUnit.Team, newUnit.Strength);

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
            yield return new WaitForSeconds(1f);

            EnemySelectUnitAt(13, 15 - _simState.TurnStateMachine.TurnCounter);
            TryMoveSelectedUnitTo(13, 14 - _simState.TurnStateMachine.TurnCounter);

            yield return new WaitForSeconds(1f);
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