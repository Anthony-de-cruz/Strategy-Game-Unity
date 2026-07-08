using System;
using System.Collections.Generic;
using System.Linq;
using Simulation.Events;

namespace Simulation
{
    ///  <summary>
    ///  Represents the current state of the game.
    ///  </summary>
    public class SimState : IDisposable
    {
        /// <summary>
        /// Width of the map.
        /// </summary>
        public uint MapWidth { get; }

        /// <summary>
        /// Height of the map.
        /// </summary>
        public uint MapHeight { get; }

        /// <summary>
        /// 2D array of tiles representing the map terrain.
        /// Dimensions should be equal to <see cref="MapWidth"/>,<see cref="MapHeight"/>.
        /// </summary>
        public Tile[,] TerrainMap { get; }

        /// <summary>
        /// 2D array of vertices representing the map height.
        /// Dimensions should be equal to <see cref="MapWidth"/> + 1,<see cref="MapHeight"/> + 1.
        /// </summary>
        public float[,] HeightMap { get; }

        /// <summary>
        /// 2D array of unit IDs representing the placement of units.
        /// Dimensions should be equal to <see cref="MapWidth"/>,<see cref="MapHeight"/>.
        /// </summary>
        public uint[,] UnitMap { get; }

        /// <summary>
        /// Turn state machine.
        /// </summary>
        public TurnStateMachine TurnStateMachine { get; }

        /// <summary>
        /// 
        /// </summary>
        private readonly EventBus _eventBus;

        /// <summary>
        /// List of units in the game.
        /// </summary>
        private readonly List<Unit> _units = new List<Unit>();

        /// <summary>
        /// Current unit counter.
        /// </summary>
        private uint _unitIdCounter = 1;

        /// <summary>
        /// Constructor for SimState.
        /// </summary>
        /// <param name="eventBus"></param>
        /// <param name="terrainMap"></param>
        /// <param name="heightMap"></param>
        /// <param name="units"></param>
        public SimState(EventBus eventBus, Tile[,] terrainMap, float[,] heightMap, MapLoader.UnitData[] units)
        {
            if (terrainMap.GetLength(0) != heightMap.GetLength(0) - 1 ||
                terrainMap.GetLength(1) != heightMap.GetLength(1) - 1)
                throw new ImpossibleStateException(
                    $"Terrain map size ({terrainMap.GetLength(0)},{terrainMap.GetLength(1)}) " +
                    $"does not align with height map size ({heightMap.GetLength(0)},{heightMap.GetLength(1)}).");

            _eventBus = eventBus;
            _eventBus.Subscribe<UnitAttackedEvent>(HandleUnitDamaged);
            _eventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChange);
            TerrainMap = terrainMap;
            HeightMap = heightMap;
            MapWidth = (uint)TerrainMap.GetLength(0);
            MapHeight = (uint)TerrainMap.GetLength(1);
            TurnStateMachine = new TurnStateMachine(_eventBus);

            UnitMap = new uint[MapWidth, MapHeight];
            foreach (MapLoader.UnitData unit in units)
                CreateUnit(unit.Team, unit.Type, unit.X, unit.Y);
        }

        /// <summary>
        ///     Detaches this game state's event handlers from the event bus.
        /// </summary>
        public void Dispose()
        {
            _eventBus.Unsubscribe<UnitAttackedEvent>(HandleUnitDamaged);
            _eventBus.Unsubscribe<TurnStateChangeEvent>(HandleTurnStateChange);
        }

        #region STATE QUERIES

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool TryGetUnit(uint id, out Unit unit)
        {
            foreach (Unit t in _units)
                if (t.Id == id)
                {
                    unit = t;
                    return true;
                }

            unit = null;
            return false;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="unitId"></param>
        /// <param name="coords"></param>
        /// <returns></returns>
        public bool TryGetUnitCoords(uint unitId, out (uint X, uint Y) coords)
        {
            for (uint x = 0; x < MapWidth; x++)
            for (uint y = 0; y < MapHeight; y++)
                if (UnitMap[x, y] == unitId)
                {
                    coords = (x, y);
                    return true;
                }

            coords = default;
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public Unit[] GetUnitsByTeam(UnitTeam team) => _units.Where(t => t.Team == team).ToArray();

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public (uint, uint)[] GetMoveableCoords(uint xCoord, uint yCoord, UnitType unitType)
        {
            var coords = new List<(uint, uint)>();

            if (xCoord >= MapWidth || yCoord >= MapHeight)
                throw new ArgumentOutOfRangeException(
                    $"Coords ({xCoord},{yCoord}) must be within map bounds ({MapWidth},{MapHeight}).");

            uint movement = Unit.GetMovementByType(unitType);
            var lowestCosts = new Dictionary<(uint, uint), uint> { [(xCoord, yCoord)] = 0 };
            var frontier = new List<((uint X, uint Y) Coords, uint Cost)> { ((xCoord, yCoord), 0) };

            while (frontier.Count > 0)
            {
                int currentIndex = 0;
                for (int i = 1; i < frontier.Count; i++)
                    if (frontier[i].Cost < frontier[currentIndex].Cost)
                        currentIndex = i;

                ((uint X, uint Y) current, uint currentCost) = frontier[currentIndex];
                frontier.RemoveAt(currentIndex);

                foreach ((uint X, uint Y) neighbour in GetAdjacentCoords(current.X, current.Y))
                {
                    if (UnitMap[neighbour.X, neighbour.Y] != 0)
                        continue;

                    uint newCost = currentCost +
                                   TileExt.GetMovementCostByType(TerrainMap[neighbour.X, neighbour.Y], unitType);
                    if (newCost > movement)
                        continue;

                    if (lowestCosts.TryGetValue(neighbour, out uint knownCost) && knownCost <= newCost)
                        continue;

                    lowestCosts[neighbour] = newCost;
                    frontier.Add((neighbour, newCost));
                }
            }

            foreach ((uint, uint) coord in lowestCosts.Keys)
                if (coord != (xCoord, yCoord))
                    coords.Add(coord);

            return coords.ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        private IEnumerable<(uint X, uint Y)> GetAdjacentCoords(uint xCoord, uint yCoord)
        {
            if (xCoord > 0)
                yield return (xCoord - 1, yCoord);
            if (xCoord + 1 < MapWidth)
                yield return (xCoord + 1, yCoord);
            if (yCoord > 0)
                yield return (xCoord, yCoord - 1);
            if (yCoord + 1 < MapHeight)
                yield return (xCoord, yCoord + 1);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="unitType"></param>
        /// <param name="unitTeam"></param>
        /// <returns></returns>
        public Unit[] GetAttackableUnitsFromCoord(uint xCoord, uint yCoord, UnitType unitType, UnitTeam unitTeam)
        {
            if (xCoord >= MapWidth || yCoord >= MapHeight)
                throw new ArgumentOutOfRangeException(
                    $"Coords ({xCoord},{yCoord}) must be within map bounds ({MapWidth},{MapHeight}).");

            var units = new List<Unit>();
            uint range = Unit.GetRangeByType(unitType);
            foreach (Unit t in _units)
            {
                if (t.Team == unitTeam) continue;
                if (!TryGetUnitCoords(t.Id, out (uint X, uint Y) targetCoords)) throw new ImpossibleStateException();
                if (targetCoords == (xCoord, yCoord)) continue;
                //if (!IsWithinRange(xCoord, yCoord, targetCoords.X, targetCoords.Y, range)) continue;
                if (!HasLineOfSight(xCoord, yCoord, targetCoords.X, targetCoords.Y, range)) continue;

                units.Add(t);
            }

            return units.ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xStart"></param>
        /// <param name="yStart"></param>
        /// <param name="xEnd"></param>
        /// <param name="yEnd"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        private static bool IsWithinRange(uint xStart, uint yStart, uint xEnd, uint yEnd, uint range)
        {
            uint xDistance = xStart > xEnd ? xStart - xEnd : xEnd - xStart;
            uint yDistance = yStart > yEnd ? yStart - yEnd : yEnd - yStart;
            return xDistance * xDistance + yDistance * yDistance <= range * range;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xStart"></param>
        /// <param name="yStart"></param>
        /// <param name="xEnd"></param>
        /// <param name="yEnd"></param>
        /// <param name="obstructionLimit"></param>
        /// <returns></returns>
        private bool HasLineOfSight(uint xStart, uint yStart, uint xEnd, uint yEnd, uint obstructionLimit)
        {
            uint obstruction = 0;
            foreach ((uint X, uint Y) coord in GetLineCoords(xStart, yStart, xEnd, yEnd))
            {
                if (coord == (xStart, yStart) || coord == (xEnd, yEnd))
                    continue;

                obstruction += TileExt.GetObstructionByType(TerrainMap[coord.X, coord.Y]);
                if (obstruction > obstructionLimit)
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     An implementation of Bresenham's Line algorithm.
        /// </summary>
        /// <param name="xStart"></param>
        /// <param name="yStart"></param>
        /// <param name="xEnd"></param>
        /// <param name="yEnd"></param>
        /// <returns></returns>
        private static IEnumerable<(uint X, uint Y)> GetLineCoords(uint xStart, uint yStart, uint xEnd, uint yEnd)
        {
            int x = (int)xStart;
            int y = (int)yStart;
            int targetX = (int)xEnd;
            int targetY = (int)yEnd;
            int xDistance = Math.Abs(targetX - x);
            int yDistance = Math.Abs(targetY - y);
            int xStep = x < targetX ? 1 : -1;
            int yStep = y < targetY ? 1 : -1;
            int error = xDistance - yDistance;

            while (true)
            {
                yield return ((uint)x, (uint)y);

                if (x == targetX && y == targetY)
                    break;

                int doubledError = 2 * error;
                if (doubledError > -yDistance)
                {
                    error -= yDistance;
                    x += xStep;
                }

                if (doubledError < xDistance)
                {
                    error += xDistance;
                    y += yStep;
                }
            }
        }

        #endregion STATE QUERIES

        #region STATE DRIVERS

        /// <summary>
        ///
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ActionMoveUnit(Unit unit, uint xCoord, uint yCoord)
        {
            switch (unit.Team)
            {
                case UnitTeam.Blue:
                    if (TurnStateMachine.State != TurnState.BlueAction)
                        throw new InvalidOperationException();
                    break;
                case UnitTeam.Red:
                    if (TurnStateMachine.State != TurnState.RedAction)
                        throw new InvalidOperationException();
                    break;
                default:
                    throw new ImpossibleStateException();
            }

            if (xCoord >= MapWidth || yCoord >= MapHeight)
                throw new ArgumentOutOfRangeException(
                    $"Coords ({xCoord},{yCoord}) must be within map bounds ({MapWidth},{MapHeight}).");

            if (unit.Actions <= 0 || UnitMap[xCoord, yCoord] != 0)
                throw new InvalidOperationException();

            // Just assume that the move is actually possible, running full Dijkstra would be far too slow.
            // In future, an exact path should be passed in which can be checked.

            TryGetUnitCoords(unit.Id, out (uint X, uint Y) oldCoords);
            UnitMap[oldCoords.X,oldCoords.Y] = 0;
            UnitMap[xCoord,yCoord] = unit.Id;
            uint oldActions = unit.Actions;
            unit.Actions -= 1;

            _eventBus.Publish(new UnitSpentActionEvent(unit.Id, oldActions, unit.Actions));
            _eventBus.Publish(new UnitMovedEvent(unit.Id, oldCoords, (xCoord, yCoord)));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="target"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ActionAttackUnit(Unit attacker, Unit target)
        {
            switch (attacker.Team)
            {
                case UnitTeam.Blue:
                    if (TurnStateMachine.State != TurnState.BlueAction)
                        throw new InvalidOperationException(
                            $"Blue unit {attacker.Id} cannot attack during {TurnStateMachine.State}");
                    break;
                case UnitTeam.Red:
                    if (TurnStateMachine.State != TurnState.RedAction)
                        throw new InvalidOperationException(
                            $"Red unit {attacker.Id} cannot attack during {TurnStateMachine.State}");
                    break;
                default:
                    throw new ImpossibleStateException();
            }

            if (attacker.Actions <= 0 ||
                attacker.Team == target.Team)
                throw new InvalidOperationException();

            uint range = Unit.GetRangeByType(attacker.Type);
            if (!TryGetUnitCoords(attacker.Id, out (uint X, uint Y) attackerCoords) ||
                !TryGetUnitCoords(target.Id, out (uint X, uint Y) targetCoords))
                throw new ImpossibleStateException();
            // if (!IsWithinRange(attackerCoords.X, attackerCoords.Y, targetCoords.X, targetCoords.Y, range) ||
            //     !HasLineOfSight(attackerCoords.X, attackerCoords.Y, targetCoords.X, targetCoords.Y, range))
            //     throw new InvalidOperationException();
            if (!HasLineOfSight(attackerCoords.X, attackerCoords.Y, targetCoords.X, targetCoords.Y, range))
                throw new InvalidOperationException();

            attacker.Actions -= 1;
            uint oldStrength = target.Strength;
            target.Strength = target.Strength > Unit.GetDamageByType(attacker.Type, target.Type)
                ? target.Strength - Unit.GetDamageByType(attacker.Type, target.Type)
                : 0;
            _eventBus.Publish(new UnitAttackedEvent(attacker.Id, target.Id, oldStrength, target.Strength));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="team"></param>
        /// <param name="type"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <exception cref="ArgumentOutOfRangeException">Invalid coordinates.</exception>
        /// <exception cref="InvalidOperationException">Coordinate already occupied.</exception>
        private void CreateUnit(UnitTeam team, UnitType type, uint xCoord, uint yCoord)
        {
            if (xCoord >= MapWidth)
                throw new ArgumentOutOfRangeException(
                    $"xCoord ({xCoord}) must be between 0 and {MapWidth}");

            if (yCoord >= MapHeight)
                throw new ArgumentOutOfRangeException(
                    $"yCoord ({yCoord}) must be between 0 and {MapHeight}");

            if (UnitMap[xCoord, yCoord] != 0)
                throw new InvalidOperationException(
                    $"Cannot create unit type {type} @ {xCoord},{yCoord}," +
                    $" tile already occupied by unit {UnitMap[xCoord, yCoord]}.");

            if (TryGetUnit(_unitIdCounter, out _))
                throw new ImpossibleStateException(
                    $"Cannot create unit {_unitIdCounter}," +
                    " this unit already exists.");

            var newUnit = new Unit(_unitIdCounter, team, type);
            _units.Add(newUnit);
            UnitMap[xCoord, yCoord] = _unitIdCounter;
            ++_unitIdCounter;
        }

        #endregion STATE DRIVERS

        #region EVENT HANDLING

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        private void HandleUnitDamaged(UnitAttackedEvent e)
        {
            if (e.NewStrength != 0) return;
            if (!TryGetUnit(e.TargetId, out Unit unit)) throw new ImpossibleStateException();
            TryGetUnitCoords(e.TargetId, out (uint X, uint Y) coords);
            UnitMap[coords.X, coords.Y] = 0;
            _units.Remove(unit);

            if (GetUnitsByTeam(unit.Team).Length != 0) return;

            // End the game if all units on either team are destroyed.
            switch (unit.Team)
            {
                case UnitTeam.Blue:
                    TurnStateMachine.RedVictory();
                    break;
                case UnitTeam.Red:
                    TurnStateMachine.BlueVictory();
                    break;
                default:
                    throw new ImpossibleStateException();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        private void HandleTurnStateChange(TurnStateChangeEvent e)
        {
            // Reset unit actions on new turn.
            switch (e.OldState)
            {
                case TurnState.RedTurn when e.NewState == TurnState.BlueTurn:
                {
                    Unit[] units = GetUnitsByTeam(UnitTeam.Blue);
                    foreach (Unit unit in units) unit.ResetActions();
                    break;
                }
                case TurnState.BlueTurn when e.NewState == TurnState.RedTurn:
                {
                    Unit[] units = GetUnitsByTeam(UnitTeam.Red);
                    foreach (Unit unit in units) unit.ResetActions();
                    break;
                }
                case TurnState.Init:
                case TurnState.BlueAction:
                case TurnState.BlueVictory:
                case TurnState.RedAction:
                case TurnState.RedVictory:
                default:
                    break;
            }
        }

        #endregion EVENT HANDLING
    }
}