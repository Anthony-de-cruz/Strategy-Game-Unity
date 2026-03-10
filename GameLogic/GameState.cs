using System;
using System.Collections.Generic;

using GameLogic.Events;
using GameLogic.MyApp.Exceptions;

namespace GameLogic
{
    /// <summary>
    /// Represents the current state of the game.
    /// </summary>
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
        /// <param name="mapX"></param>
        /// <param name="mapY"></param>
        public GameState(uint mapX, uint mapY)
        {
            MapX = mapX;
            MapY = mapY;
            
            EventBus = new EventBus();
            TurnStateMachine = new TurnStateMachine(EventBus);

            // Initialize the map
            Map = new Tile[mapX][];
            for (int x = 0; x < mapX; x++)
            {
                Map[x] = new Tile[mapY];
                for (int y = 0; y < mapY; y++)
                {
                    Map[x][y] = new Tile(TileType.Grassland, 0);
                }
            }
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
    }
}
