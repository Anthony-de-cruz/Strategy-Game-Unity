using UnityEngine;

using GameLogic;
using GameLogic.Events;

public class UnitFactory
{
    EventBus _eventBus;

    public UnitFactory(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Unit CreateUnit(uint id)
    {
        Unit newUnit =  new(id, UnitTeam.Blue, UnitType.Infantry, 5, _eventBus);

        // Create associated game object.

        return newUnit;
    }
}
