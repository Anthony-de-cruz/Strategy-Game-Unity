using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Events;
using GameLogic.MyApp.Exceptions;

namespace GameLogic
{
    ///  <summary>
    ///  Represents the current state of the game.
    ///  </summary>
    public class GameState
    {
        /// <summary>
        /// Width of the map.
        /// </summary>
        public uint MapX { get; }

        /// <summary>
        /// Height of the map.
        /// </summary>
        public uint MapY { get; }

        /// <summary>
        /// 2D array of tiles representing the map.
        /// </summary>
        public Tile[][] Map { get; }

        /// <summary>
        /// Turn state machine.
        /// </summary>
        public TurnStateMachine TurnStateMachine { get; }

        /// <summary>
        /// 
        /// </summary>
        public EventBus EventBus { get; }

        /// <summary>
        /// List of units in the game.
        /// </summary>
        private readonly List<Unit> _units = new List<Unit>();

        /// <summary>
        ///     Current unit counter.
        /// </summary>
        private uint _unitIdCounter = 1;


        /// <summary>
        /// Constructor for GameState.
        /// </summary>
        /// <param name="mapJsonString"></param>
        public GameState(string mapJsonString)
        {
            (Map, MapX, MapY) = MapLoader.LoadFromJson(mapJsonString);
            EventBus = new EventBus();
            TurnStateMachine = new TurnStateMachine(EventBus);
            EventBus.Subscribe<UnitDamagedEvent>(HandleUnitDamaged);
            EventBus.Subscribe<TurnStateChangeEvent>(HandleTurnStateChange);

            for (var i = 0; i < 3; i++) CreateUnit(UnitTeam.Blue, UnitType.Infantry, i + 12, 10);
            for (var i = 0; i < 2; i++) CreateUnit(UnitTeam.Blue, UnitType.Tank, i + 10, 9);
            for (var i = 0; i < 3; i++) CreateUnit(UnitTeam.Red, UnitType.Tank, i + 12, 15);
            for (var i = 0; i < 1; i++) CreateUnit(UnitTeam.Red, UnitType.Infantry, i + 16, 16);
        }

        ///////////////////
        // STATE QUERIES //
        ///////////////////

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
            for (uint x = 0; x < MapX; x++)
            for (uint y = 0; y < MapY; y++)
                if (Map[x][y].UnitId == unitId)
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

            if (xCoord >= MapX || yCoord >= MapY)
                throw new ArgumentOutOfRangeException(
                    $"Coords ({xCoord},{yCoord}) must be within map bounds ({MapX},{MapY}).");

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
                    Tile tile = Map[neighbour.X][neighbour.Y];
                    if (tile.UnitId != 0)
                        continue;

                    uint newCost = currentCost + Tile.GetMovementCostByType(tile.Type, unitType);
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
            if (xCoord + 1 < MapX)
                yield return (xCoord + 1, yCoord);
            if (yCoord > 0)
                yield return (xCoord, yCoord - 1);
            if (yCoord + 1 < MapY)
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
            if (xCoord >= MapX || yCoord >= MapY)
                throw new ArgumentOutOfRangeException(
                    $"Coords ({xCoord},{yCoord}) must be within map bounds ({MapX},{MapY}).");

            var units = new List<Unit>();
            uint range = Unit.GetRangeByType(unitType);
            foreach (Unit t in _units)
            {
                if (t.Team == unitTeam) continue;
                if (!TryGetUnitCoords(t.Id, out (uint X, uint Y) targetCoords)) throw new ImpossibleStateException();
                if (targetCoords == (xCoord, yCoord)) continue;
                if (!IsWithinRange(xCoord, yCoord, targetCoords.X, targetCoords.Y, range)) continue;
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

                obstruction += Tile.GetObstructionByType(Map[coord.X][coord.Y].Type);
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

        ///////////////////
        // STATE DRIVERS //
        ///////////////////

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
                    if (TurnStateMachine.State != TurnState.BlueTurn)
                        throw new InvalidOperationException();
                    break;
                case UnitTeam.Red:
                    if (TurnStateMachine.State != TurnState.RedTurn)
                        throw new InvalidOperationException();
                    break;
                default:
                    throw new ImpossibleStateException();
            }

            if (xCoord >= MapX || yCoord >= MapY)
                throw new ArgumentOutOfRangeException(
                    $"Coords ({xCoord},{yCoord}) must be within map bounds ({MapX},{MapY}).");

            if (unit.Actions <= 0 ||
                Map[xCoord][yCoord].UnitId != 0)
                throw new InvalidOperationException();

            // Just assume that the move is actually possible, running full Dijkstra would be far too slow.
            // In future, an exact path should be passed in which can be checked.

            TurnStateMachine.BeginAction();

            TryGetUnitCoords(unit.Id, out (uint X, uint Y) oldCoords);
            Map[oldCoords.X][oldCoords.Y].UnitId = 0;
            Map[xCoord][yCoord].UnitId = unit.Id;
            unit.Actions -= 1;
            EventBus.Publish(new UnitMovedEvent(unit.Id, oldCoords, (xCoord, yCoord)));

            TurnStateMachine.EndAction();
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
                    if (TurnStateMachine.State != TurnState.BlueTurn)
                        throw new InvalidOperationException();
                    break;
                case UnitTeam.Red:
                    if (TurnStateMachine.State != TurnState.RedTurn)
                        throw new InvalidOperationException();
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
            if (!IsWithinRange(attackerCoords.X, attackerCoords.Y, targetCoords.X, targetCoords.Y, range) ||
                !HasLineOfSight(attackerCoords.X, attackerCoords.Y, targetCoords.X, targetCoords.Y, range))
                throw new InvalidOperationException();

            TurnStateMachine.BeginAction();

            attacker.Actions -= 1;
            target.Strength = target.Strength > Unit.GetDamageByType(attacker.Type, target.Type)
                ? target.Strength - Unit.GetDamageByType(attacker.Type, target.Type)
                : 0;

            TurnStateMachine.EndAction();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="team"></param>
        /// <param name="type"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid coordinates.</exception>
        /// <exception cref="InvalidOperationException">Coordinate already occupied.</exception>
        public Unit CreateUnit(UnitTeam team, UnitType type, int xCoord, int yCoord)
        {
            if (xCoord < 0 || xCoord >= Map.Length)
                throw new ArgumentOutOfRangeException(
                    $"xCoord ({xCoord}) must be between 0 and {Map.Length}");

            if (yCoord < 0 || yCoord >= Map[0].Length)
                throw new ArgumentOutOfRangeException(
                    $"yCoord ({yCoord}) must be between 0 and {Map[0].Length}");

            if (Map[xCoord][yCoord].UnitId != 0)
                throw new InvalidOperationException(
                    $"Cannot create unit type {type} @ {xCoord},{yCoord}," +
                    $" tile already occupied by unit {Map[xCoord][yCoord].UnitId}.");

            if (TryGetUnit(_unitIdCounter, out _))
                throw new ImpossibleStateException(
                    $"Cannot create unit {_unitIdCounter}," +
                    " this unit already exists.");

            var newUnit = new Unit(_unitIdCounter, team, type, EventBus);
            _units.Add(newUnit);
            Map[xCoord][yCoord].UnitId = _unitIdCounter;
            ++_unitIdCounter;
            return newUnit;
        }

        ////////////////////
        // EVENT HANDLING //
        ////////////////////

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        private void HandleUnitDamaged(UnitDamagedEvent e)
        {
            if (e.NewStrength != 0) return;
            if (!TryGetUnit(e.UnitId, out Unit unit)) throw new ImpossibleStateException();
            TryGetUnitCoords(e.UnitId, out (uint X, uint Y) coords);
            Map[coords.X][coords.Y].UnitId = 0;
            _units.Remove(unit);
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
    }
}