using System;
using System.Collections.Generic;
using GameLogic;
using GameLogic.MyApp.Exceptions;
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

        public Color highlightSelectionColour = new Color(0.9f, 0.8f, 0f, 0.45f);
        public Color highlightMovementColour  = new Color(0.1f, 0.9f, 1f, 0.45f);
        public Color highlightTargetColour = new Color(1f, 0.1f, 0f, 0.45f);
        public TilePrefab[] tilePrefabs;
        public Transform tileParent;

        private readonly Dictionary<TileType, List<GameObject>> _prefabsByType = new();
        private GameObject[][] _spawnedTiles;
        private Renderer[][] _spawnedTilesHighlights;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

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
        ///
        /// </summary>
        /// <param name="material"></param>
        private static void EnableEmission(Material material)
        {
            if (material == null) return;
            material.EnableKeyword("_EMISSION");
        }

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            Render();

            simController.OnHighlightMovement += HandleHighlightMovement;
            simController.OnHighlightTargets += HandleHighlightTargets;
            simController.OnHighlightSelection += HandleHighlightSelection;
            simController.OnResetHighlight += HandleHighlightReset;
        }

        private void OnDisable()
        {
            simController.OnHighlightMovement -= HandleHighlightMovement;
            simController.OnHighlightTargets -= HandleHighlightTargets;
            simController.OnHighlightSelection -= HandleHighlightSelection;
            simController.OnResetHighlight -= HandleHighlightReset;
        }

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void Render()
        {
            _spawnedTiles = new GameObject[simController.MapX][];
            _spawnedTilesHighlights = new Renderer[simController.MapX][];
            for (var x = 0; x < simController.MapX; x++)
            {
                _spawnedTiles[x] = new GameObject[simController.MapY];
                _spawnedTilesHighlights[x] = new Renderer[simController.MapY];
                for (var y = 0; y < simController.MapY; y++)
                {
                    Tile tile = simController.Map[x][y];

                    // Get prefab tile.
                    if (!_prefabsByType.TryGetValue(tile.Type, out List<GameObject> prefabSet))
                        throw new InvalidOperationException($"No prefab assigned for tile type {tile.Type}");
                    GameObject prefab = prefabSet[UnityEngine.Random.Range(0, prefabSet.Count)];

                    // Instantiate prefab.
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
                    var highlightPlaneRenderer = tileObject
                        .transform
                        .Find("HighlightPlane")
                        .GetComponent<Renderer>();
                    EnableEmission(highlightPlaneRenderer.sharedMaterial);

                    _spawnedTiles[x][y] = tileObject;
                    _spawnedTilesHighlights[x][y] = highlightPlaneRenderer;
                    tileObject.name = $"Tile_{tile.Type}_{x}:{y}";
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="color"></param>
        private void RenderTileHighlight((uint, uint)[] tiles, Color color)
        {
            var highlightBlock = new MaterialPropertyBlock();
            highlightBlock.SetColor(EmissionColorId, color);

            foreach ((uint xCoord, uint yCoord) in tiles)
            {
                Renderer highlightRenderer = _spawnedTilesHighlights[xCoord][yCoord];

                highlightRenderer.SetPropertyBlock(highlightBlock);
                highlightRenderer.enabled = true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tiles"></param>
        private void HandleHighlightMovement((uint, uint)[] tiles)
        {
            RenderTileHighlight(tiles, highlightMovementColour);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="targets"></param>
        private void HandleHighlightTargets(UnitView[] targets)
        {
            var targetsCoords = new (uint, uint)[targets.Length];
            for (var i = 0; i < targets.Length; i++)
                targetsCoords[i] = (targets[i].X, targets[i].Y);
            RenderTileHighlight(targetsCoords, highlightTargetColour);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tile"></param>
        private void HandleHighlightSelection((uint, uint) tile)
        {
            RenderTileHighlight(new[] { tile }, highlightSelectionColour);
        }

        /// <summary>
        ///
        /// </summary>
        private void HandleHighlightReset()
        {
            // Todo - Would be more efficient to cache which ones are enabled.
            for (var x = 0; x < simController.MapX; x++)
            for (var y = 0; y < simController.MapY; y++)
                _spawnedTilesHighlights[x][y].enabled = false;
        }
    }
}
