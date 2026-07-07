using Simulation;
using Simulation.Events;

namespace Simulation.Cli;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Todo - Replace with relative path.
        var reader =
            new StreamReader(
                File.OpenRead(
                    @"C:\Users\Anthony\Projects\Strategy-Game-Unity\Simulation.Tests\TestMaps\map0.json"));
        string jsonString = reader.ReadToEnd();
        reader.Close();

        (
            string mapName,
            uint mapX,
            uint mapY,
            MapLoader.UnitData[] units
        ) = MapLoader.LoadMetaFromJson(jsonString);

        var heightMapRaw = new Span<byte>(new byte[mapX * mapY * 4]);
        File.OpenRead(@"C:\Users\Anthony\Projects\Strategy-Game-Unity\Simulation.Tests\TestMaps\map0height.raw")
            .ReadExactly(heightMapRaw);
        reader.Close();

        var terrainMapRaw = new Span<byte>(new byte[mapX * mapY * 4]);
        File.OpenRead(@"C:\Users\Anthony\Projects\Strategy-Game-Unity\Simulation.Tests\TestMaps\map0terrain.raw")
            .ReadExactly(terrainMapRaw);
        reader.Close();

        TileType[,] terrainMap = MapLoader.LoadTerrainMapFromRaw(terrainMapRaw, mapX, mapY);
        float[,] heightMap = MapLoader.LoadHeightMapFromRaw(heightMapRaw, mapX, mapY, 10);

        SimState simState = new(new EventBus(), terrainMap, heightMap, units);
        Ai ai = new(simState);

        // (uint, uint)[] coords = simState.GetMoveableCoords(2, 2, UnitType.Infantry);
        // foreach ((uint, uint) coord in coords)
        //     Console.WriteLine(coord);

    }
}