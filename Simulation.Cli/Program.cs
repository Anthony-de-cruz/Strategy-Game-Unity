using Simulation.Events;

namespace Simulation.Cli;

internal static class Program
{
    private static void Main(string[] args)
    {
        (
            string _,
            uint mapX,
            uint mapY,
            MapLoader.UnitData[] units
        ) = MapLoader.LoadMetaFromJson(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestMaps", "map0.json")));

        var terrainMapRaw = new Span<byte>(new byte[mapX * mapY * 4]);
        var heightMapRaw = new Span<byte>(new byte[mapX * mapY * 4]);

        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestMaps", "map0terrain.raw"))
            .ReadExactly(terrainMapRaw);
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestMaps", "map0height.raw"))
            .ReadExactly(heightMapRaw);

        Tile[,] terrainMap = MapLoader.LoadTerrainMapFromRaw(terrainMapRaw, mapX, mapY);
        float[,] heightMap = MapLoader.LoadHeightMapFromRaw(heightMapRaw, mapX, mapY, 10);

        EventBus eventBus = new();
        SimState simState = new(eventBus, terrainMap, heightMap, units);
        Ai ai = new(simState, eventBus);

        // (uint, uint)[] coords = simState.GetMoveableCoords(2, 2, UnitType.Infantry);
        // foreach ((uint, uint) coord in coords)
        //     Console.WriteLine(coord);
    }
}