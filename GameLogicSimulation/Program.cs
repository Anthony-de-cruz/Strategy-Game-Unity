using GameLogic;
using GameLogic.Events;

namespace GameLogicSimulation;

internal class Program
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

        var coords = gameState.GetMoveableCoords(2, 2, UnitType.Infantry);
        foreach (var coord in coords)
            Console.WriteLine(coord);

        var heightMapRaw = new Span<byte>(new byte[200]);
        File.OpenRead(@"C:\Users\Anthony\Projects\Strategy-Game-Unity\Unity2\Assets\StreamingAssets\heightmap.raw")
            .ReadExactly(heightMapRaw);
        reader.Close();

        ushort[] heightMapValues = MapLoader.LoadHeightMapFromRaw(heightMapRaw, 10, 10);

    }
}