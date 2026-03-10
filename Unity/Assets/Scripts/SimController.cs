using System;
using System.Collections;
using System.Collections.Generic;
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
        /// 
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        public bool SelectUnitAt(int xCoord, int yCoord)
        {
            if (TurnState != TurnState.BlueTurn) return false;
            uint id = _simState.Map[xCoord][yCoord].UnitId;
            if (id == 0)
            {
                SelectedId = 0;
                return false;
            }
            if (!_simState.TryGetUnit(id, out Unit unit)) throw new ImpossibleStateException();
            if (unit.Team != _clientTeam) return false;
                
            SelectedId = id;
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
        public bool MoveSelectedUnitTo(int xCoord, int yCoord)
        {
            if (SelectedId == 0) return false;
            if (_simState.Map[xCoord][yCoord].UnitId != 0) return false;
            if (!_unitObjects.TryGetValue(SelectedId, out GameObject unit))
                throw new ImpossibleStateException();
            
            _simState.Map[_selectedXCoord][_selectedYCoord].UnitId = 0;
            _simState.Map[xCoord][yCoord].UnitId = SelectedId;
            
            unit.transform.position = new Vector3(xCoord * WORLD_SCALE + 4, 0.5f, yCoord * WORLD_SCALE + 4);

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
            MoveSelectedUnitTo(13, 14 - _simState.TurnStateMachine.TurnCounter);
            
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
    }
}