using System;
using System.Collections.Generic;
using GameLogic;
using GameLogic.Events;
using UnityEngine;


namespace Assets.Scripts
{
    public class UnitRenderer : MonoBehaviour
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

            simController.OnUnitDamaged += HandleUnitDamaged;
            simController.OnUnitMoved += HandleUnitMoved;
        }

        /// <summary>
        ///
        /// </summary>
        private void OnDisable()
        {
            simController.OnUnitDamaged -= HandleUnitDamaged;
            simController.OnUnitMoved -= HandleUnitMoved;

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
                new Vector3(unit.X * SimController.WORLD_SCALE + 4, 0.5f, unit.Y * SimController.WORLD_SCALE + 4),
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
                unit.obj.transform.position = new Vector3(
                    e.NewCoords.Item1 * SimController.WORLD_SCALE + 4, 0.5f,
                    e.NewCoords.Item2 * SimController.WORLD_SCALE + 4);
                return;
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void HandleUnitDamaged(UnitDamagedEvent e)
        {
            if (e.NewStrength > 0) return;
            foreach ((uint id, GameObject obj) unit in _spawnedUnits)
            {
                if (unit.id != e.UnitId) continue;
                Destroy(unit.obj);
                _spawnedUnits.Remove(unit);
                return;
            }
        }
    }
}