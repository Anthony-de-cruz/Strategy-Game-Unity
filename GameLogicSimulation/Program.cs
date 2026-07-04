using GameLogic;
using GameLogic.Events;

namespace GameLogicSimulation;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Todo - Replace with relative path.
        var reader =
            new StreamReader(
                File.OpenRead(
                    @"C:\Users\Anthony\Projects\Strategy-Game-Unity\Unity2\Assets\StreamingAssets\Maps\map0.json"));
        string jsonString = reader.ReadToEnd();
        reader.Close();

        GameState gameState = new(new EventBus(), jsonString);
        Ai ai = new(gameState);

        (uint, uint)[] coords = gameState.GetMoveableCoords(2, 2, UnitType.Infantry);
        foreach ((uint, uint) coord in coords)
            Console.WriteLine(coord);

        var heightMapRaw = new Span<byte>(new byte[(50 * 50) * 4]);
        File.OpenRead(@"C:\Users\Anthony\Projects\Strategy-Game-Unity\GameLogicTests\TestMaps\map0height.raw")
            .ReadExactly(heightMapRaw);
        reader.Close();

        uint[,] heightMapValues = MapLoader.LoadHeightMapFromRaw(heightMapRaw, 50, 50, 10);
    }
}