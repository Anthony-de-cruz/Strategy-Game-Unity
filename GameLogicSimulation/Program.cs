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
                File.OpenRead(@"C:\Users\Anthony\University\Game-Development\Strategy-Game-Unity\map.json"));
        string jsonString = reader.ReadToEnd();
        reader.Close();

        GameState gameState = new(jsonString);
    }
}