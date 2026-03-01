namespace GameLogic
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
    }
}
