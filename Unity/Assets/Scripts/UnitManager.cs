using System;
using System.Collections.Generic;
using GameLogic;
using GameLogic.Events;
using GameLogic.MyApp.Exceptions;
using UnityEngine;


namespace Assets.Scripts
{
    public class UnitController : MonoBehaviour
    {
        public SimController simController;

        public GameObject prefabInfantryBlue;
        public GameObject prefabInfantryRed;
        public GameObject prefabTankBlue;
        public GameObject prefabTankRed;
        public GameObject prefabUnitLabel;

        private readonly List<(uint, GameObject)> _spawnedUnits = new();

        /// <summary>
        ///
        /// </summary>
        private void OnEnable()
        {
            foreach (UnitView unit in simController.GetUnitsByTeam(UnitTeam.Blue))
                RenderUnit(unit);
            foreach (UnitView unit in simController.GetUnitsByTeam(UnitTeam.Red))
                RenderUnit(unit);

            simController.OnUnitDamaged += HandleUnitAttacked;
            simController.OnUnitMoved += HandleUnitMoved;
            simController.OnStateReset += HandleStateReset;
        }

        /// <summary>
        ///
        /// </summary>
        private void OnDisable()
        {
            simController.OnUnitDamaged -= HandleUnitAttacked;
            simController.OnUnitMoved -= HandleUnitMoved;
            simController.OnStateReset -= HandleStateReset;

            foreach ((uint Id, GameObject Obj) unit in _spawnedUnits)
                Destroy(unit.Obj);
            _spawnedUnits.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        private void RenderUnit(UnitView unit)
        {
            GameObject prefab = (unit.Type, unit.Team) switch
            {
                (UnitType.Infantry, UnitTeam.Blue) => prefabInfantryBlue,
                (UnitType.Infantry, UnitTeam.Red) => prefabInfantryRed,
                (UnitType.Tank, UnitTeam.Blue) => prefabTankBlue,
                (UnitType.Tank, UnitTeam.Red) => prefabTankRed,
                _ => throw new NotImplementedException()
            };

            Quaternion rotation = unit.Team switch
            {
                UnitTeam.Blue => Quaternion.Euler(0f, 0f, 0f),
                UnitTeam.Red => Quaternion.Euler(0f, 180f, 0f),
                _ => throw new NotImplementedException()
            };

            GameObject modelObj = Instantiate(
                prefab,
                new Vector3(unit.X * SimController.WorldScale + 5, 0.05f, unit.Y * SimController.WorldScale + 4),
                rotation,
                transform);
            _spawnedUnits.Add((unit.Id, modelObj));

            GameObject labelObj = Instantiate(prefabUnitLabel, modelObj.transform);
            labelObj.transform.localPosition = new Vector3(0f, 10f, 0f);
            var labelMono = labelObj.GetComponent<UnitLabel>();
            labelMono.Init(simController, unit.Id, unit.Type, unit.Team, unit.Strength);

            Debug.Log(
                $"Unit {unit.Id} of type {unit.Type} instantiated" +
                $" @ {unit.X},{unit.Y}/{modelObj.transform.position}");
        }

        /// <summary>
        ///
        /// </summary>
        private void HandleUnitMoved(UnitMovedEvent e)
        {
            foreach ((uint id, GameObject obj) unit in _spawnedUnits)
            {
                if (unit.id != e.UnitId) continue;
                if (!simController.TryGetUnitById(unit.id, out UnitView view)) throw new InvalidConfigException();

                switch (view.Type)
                {
                    case UnitType.Infantry:
                        var unitInf = unit.obj.GetComponent<UnitInfantry>();
                        StartCoroutine(
                            unitInf.MoveTo(new Vector3(
                                e.NewCoords.Item1 * SimController.WorldScale + 5f, 0.05f,
                                e.NewCoords.Item2 * SimController.WorldScale + 5f))
                        );
                        break;
                    case UnitType.Tank:
                        var unitTank = unit.obj.GetComponent<UnitTank>();
                        StartCoroutine(
                            unitTank.MoveTo(new Vector3(
                                e.NewCoords.Item1 * SimController.WorldScale + 5f, 0.05f,
                                e.NewCoords.Item2 * SimController.WorldScale + 5f))
                        );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return;
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void HandleUnitAttacked(UnitAttackedEvent e)
        {
            // Remove destroyed unit.
            if (e.NewStrength == 0)
            {
                foreach ((uint id, GameObject obj) unit in _spawnedUnits)
                {
                    if (unit.id != e.TargetId) continue;
                    Destroy(unit.obj);
                    _spawnedUnits.Remove(unit);
                    break;
                }
            }

            // Start unit attack animations.
            foreach ((uint id, GameObject obj) unit in _spawnedUnits)
            {
                if (unit.id != e.AttackerId) continue;
                if (!simController.TryGetUnitById(unit.id, out UnitView view)) throw new InvalidConfigException();
                switch (view.Type)
                {
                    case UnitType.Infantry:
                        var unitInf = unit.obj.GetComponent<UnitInfantry>();
                        StartCoroutine(unitInf.Attack(new Vector3()));
                        break;
                    case UnitType.Tank:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void HandleStateReset()
        {
            foreach ((uint Id, GameObject Obj) unit in _spawnedUnits)
                Destroy(unit.Obj);
            _spawnedUnits.Clear();

            foreach (UnitView unit in simController.GetUnitsByTeam(UnitTeam.Blue))
                RenderUnit(unit);
            foreach (UnitView unit in simController.GetUnitsByTeam(UnitTeam.Red))
                RenderUnit(unit);
        }
    }
}