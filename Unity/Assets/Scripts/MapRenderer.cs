using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// 
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        public SimController simController;

        [Serializable]
        public struct TilePrefab
        {
            public TileType type;
            public GameObject prefab;
        }

        public TilePrefab[] tilePrefabs;
        public Transform tileParent;

        private readonly Dictionary<TileType, List<GameObject>> _prefabsByType = new();
        private readonly List<GameObject> _spawnedTiles = new();

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            foreach (TilePrefab entry in tilePrefabs)
            {
                if (_prefabsByType.TryGetValue(entry.type, out List<GameObject> value))
                    value.Add(entry.prefab);
                else
                    _prefabsByType[entry.type] = new List<GameObject> { entry.prefab };
            }

            if (tileParent == null)
                tileParent = transform;
        }

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            Render();
        }

        /// <summary>
        ///     Called on game object disabled.
        /// </summary>
        private void OnDisable()
        {
            Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void Render()
        {
            Tile[][] map = simController.GetMap();
            for (var x = 0; x < map[0].Length; x++)
            {
                for (var y = 0; y < map[0].Length; y++)
                {
                    Tile tile = map[x][y];

                    if (!_prefabsByType.TryGetValue(tile.Type, out List<GameObject> prefabSet))
                        throw new InvalidOperationException($"No prefab assigned for tile type {tile.Type}");

                    GameObject prefab = prefabSet[UnityEngine.Random.Range(0, prefabSet.Count)];

                    Vector3 position = new(
                        x * SimController.WORLD_SCALE + SimController.WORLD_SCALE * 0.5f,
                        0f,
                        y * SimController.WORLD_SCALE + SimController.WORLD_SCALE * 0.5f
                    );

                    GameObject tileObject = Instantiate(
                        prefab,
                        position,
                        // Random rotation.
                        Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90f, 0f),
                        tileParent
                    );

                    tileObject.name = $"Tile_{tile.Type}_{x}:{y}";
                    _spawnedTiles.Add(tileObject);
                }
            }
        }

        /// <summary>
        ///     Destroy all rendered objects.
        /// </summary>
        private void Clear()
        {
            foreach (GameObject tile in _spawnedTiles.Where(tile => tile != null))
                Destroy(tile);

            _spawnedTiles.Clear();
        }
    }
}
