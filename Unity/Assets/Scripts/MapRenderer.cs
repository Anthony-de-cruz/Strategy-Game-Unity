using System;
using System.Collections.Generic;
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
        private GameObject[][] _spawnedTiles;

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
        ///
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void Render()
        {
            _spawnedTiles = new GameObject[simController.MapX][];
            for (var x = 0; x < simController.MapX; x++)
            {
                _spawnedTiles[x] = new GameObject[simController.MapY];
                for (var y = 0; y < simController.MapY; y++)
                {
                    Tile tile = simController.Map[x][y];

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
                    _spawnedTiles[x][y] = tileObject;

                    tileObject.name = $"Tile_{tile.Type}_{x}:{y}";
                }
            }
        }
    }
}
