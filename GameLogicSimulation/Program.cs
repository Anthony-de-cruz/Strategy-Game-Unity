using GameLogic;
using GameLogic.Events;

namespace GameLogicSimulation;

internal class Program
{
    private static void Main(string[] args)
    {
        EventBus eventBus = new();
        GameState gameState = new(25, 25);
    }
}
