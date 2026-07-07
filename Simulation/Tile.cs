namespace Simulation
{
    /// <summary>
    /// Types of tiles on the map.
    /// </summary>
    public enum Tile
    {
        Paved,
        Grassland,
        Woodland,
        Building
    }

    /// <summary>
    /// Extension for <see cref="Tile"/>.
    /// </summary>
    public static class TileExt
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public static uint GetObstructionByType(Tile tile)
        {
            return tile switch
            {
                Tile.Paved => 0,
                Tile.Grassland => 0,
                Tile.Woodland => 3,
                Tile.Building => 99,
                _ => throw new ImpossibleStateException()
            };
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public static uint GetMovementCostByType(Tile tile, UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Infantry => tile switch
                {
                    Tile.Paved => 2,
                    Tile.Grassland => 2,
                    Tile.Woodland => 2,
                    Tile.Building => 2,
                    _ => throw new ImpossibleStateException()
                },
                UnitType.Tank => tile switch
                {
                    Tile.Paved => 1,
                    Tile.Grassland => 2,
                    Tile.Woodland => 4,
                    Tile.Building => 100,
                    _ => throw new ImpossibleStateException()
                },
                _ => throw new ImpossibleStateException()
            };
        }
    }
}