using UnityEngine;

using GameLogic;
using GameLogic.Events;
using System;

/// <summary>
/// 
/// </summary>
public class GameController : MonoBehaviour
{
    private EventBus _eventBus;
    private GameState _gameState;
    private uint _unitId = 0;

    public GameObject infantryPrefab;
    public Material infantryBlueMat;
    public Material infantryRedMat;

    /// <summary>
    /// Called on script load.
    /// </summary>
    void Awake()
    {
        _eventBus = new EventBus();
        _eventBus.Subscribe<UnitDamagedEvent>(OnUnitDamaged);

        _gameState = new GameState(20, 20, _eventBus);

        for (int i = 0; i < 3; i++) {
            CreateUnit(UnitTeam.Blue, UnitType.Infantry, i, 2);
        }

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
    /// 
    /// </summary>
    /// <param name="team"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public Unit CreateUnit(UnitTeam team, UnitType type, int xCoord, int yCoord)
    {
        Unit newUnit = new(_unitId, team, type, _eventBus);
        _gameState.AddUnit(newUnit);
        _gameState.Map[xCoord][yCoord].UnitId = _unitId;

        GameObject obj = Instantiate(infantryPrefab, new Vector3(xCoord + 3, 0, 0), Quaternion.identity);
        obj.GetComponentInChildren<Renderer>().material = team switch
        {
            UnitTeam.Blue => infantryBlueMat,
            UnitTeam.Red => infantryRedMat,
            _ => throw new NotImplementedException(),
        };

        Debug.Log($"Unit {_unitId} Instantiated.");
        _unitId++;

        return newUnit;
    }


    /// <summary>
    /// Handles raised <see cref="UnitDamagedEvent"/>.
    /// </summary>
    /// <param name="gameEvent"></param>
    void OnUnitDamaged(UnitDamagedEvent gameEvent)
    {
        Debug.Log("Unit Damaged Event");
    }
}
