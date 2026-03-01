using System.Collections.Generic;

using GameLogic.Events;

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
        /// List of units in the game.
        /// </summary>
        public List<Unit> Units { get; } = new List<Unit>();

        /// <summary>
        /// Internal turn state machine.
        /// </summary>
        private readonly TurnStateMachine _turnStateMachine;

        /// <summary>
        /// Constructor for GameState.
        /// </summary>
        public GameState(uint mapX, uint mapY, EventBus eventBus)
        {
            MapX = mapX;
            MapY = mapY;

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

            _turnStateMachine = new TurnStateMachine(eventBus);

            // Initialize turn order
            _turnStateMachine.Init();     // Blue turn
            _turnStateMachine.EndTurn();  // Red turn
            _turnStateMachine.EndTurn();  // Blue turn
        }
    }
}
