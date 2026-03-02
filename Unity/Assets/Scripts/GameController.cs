using UnityEngine;

using GameLogic;
using GameLogic.Events;

/// <summary>
/// 
/// </summary>
public class GameController : MonoBehaviour
{
    private EventBus _eventBus;
    private GameState _gameState;

    /// <summary>
    /// Called on script load.
    /// </summary>
    void Awake()
    {
        _eventBus = new EventBus();
        _eventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);

        _gameState = new GameState(20, 20, _eventBus);

        for (uint i = 1; i < 3; i++) {
            _gameState.AddUnit(new Unit(i, UnitTeam.Blue, UnitType.Infantry, 10, _eventBus));
        }

        _gameState.Map[2][2].UnitId = 1;

        _gameState.TryGetUnit(1, out Unit unit);

        unit.Strength -= 2;
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        
    }

    /// <summary>
    /// Handles raised <see cref="UnitDamagedEvent"/>.
    /// </summary>
    /// <param name="gameEvent"></param>
    void OnUnitDamaged(UnitDamagedEvent gameEvent)
    {
        Debug.Log("DAMAGED EVENT");
    }
}
