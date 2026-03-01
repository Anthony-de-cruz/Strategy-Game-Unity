using UnityEngine;

using GameLogic;
using GameLogic.Events;

public class GameController : MonoBehaviour
{
    private EventBus _eventBus;
    private GameState _gameState;

    void Start()
    {
        _eventBus = new EventBus(); 
        _gameState = new GameState(1, 2, _eventBus);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
