namespace Simulation
{
    /// <summary>
    /// Types of tiles on the map.
    /// </summary>
    public enum TileType
    {
        Paved,
        Grassland,
        Woodland,
        Building
    }

    /// <summary>
    /// Represents a single tile on the map.
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// Type of this tile.
        /// </summary>
        public TileType Type { get; }

        /// <summary>
        /// ID of the unit on this tile (if any).
        /// </summary>
        public uint UnitId { get; set; }

        /// <summary>
        /// Constructor for <see cref="Tile"/>.
        /// </summary>
        /// <param name="type">Tile type.</param>
        /// <param name="unitId">ID of the unit on this tile.</param>
        public Tile(TileType type, uint unitId)
        {
            Type = type;
            UnitId = unitId;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tileType"></param>
        /// <returns></returns>
        public static uint GetObstructionByType(TileType tileType)
        {
            return tileType switch
            {
                TileType.Paved => 0,
                TileType.Grassland => 0,
                TileType.Woodland => 3,
                TileType.Building => 99,
                _ => throw new ImpossibleStateException()
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tileType"></param>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public static uint GetMovementCostByType(TileType tileType, UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Infantry => tileType switch
                {
                    TileType.Paved => 2,
                    TileType.Grassland => 2,
                    TileType.Woodland => 2,
                    TileType.Building => 2,
                    _ => throw new ImpossibleStateException()
                },
                UnitType.Tank => tileType switch
                {
                    TileType.Paved => 1,
                    TileType.Grassland => 2,
                    TileType.Woodland => 4,
                    TileType.Building => 100,
                    _ => throw new ImpossibleStateException()
                },
                _ => throw new ImpossibleStateException()
            };
        }
    }
}