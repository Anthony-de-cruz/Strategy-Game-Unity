using System;
using System.Collections.Generic;
using System.Linq;
using Simulation;
using UnityEngine;

namespace Assets.Scripts
{
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] private SimController simController;
        [SerializeField] private TileMaterial[] tileMaterials;
        [SerializeField] private GameObject[] buildingPrefabs;
        [SerializeField] private GameObject[] treePrefabs;

        private readonly Tile[] _tileTypes =
            Enum.GetValues(typeof(Tile))
                .Cast<Tile>()
                .ToArray();

        private readonly List<GameObject> _terrainDetails = new();
        private MeshFilter _terrainMeshFilter;

        /// <summary>
        ///     Type for configuration.
        /// </summary>
        [Serializable]
        public struct TileMaterial
        {
            public Tile type;
            public Material material;
        }

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            ValidateConfig();
            // Create mesh.
            _terrainMeshFilter = gameObject.AddComponent<MeshFilter>();
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Map tileMaterials to mesh renderer shared
            // materials where the index is the enum value.
            var materials = new Material[_tileTypes.Length];
            for (var i = 0; i < _tileTypes.Length; i++)
            {
                foreach (TileMaterial tile in tileMaterials)
                {
                    if (tile.type != _tileTypes[i]) continue;
                    materials[i] = tile.material;
                    break;
                }

                if (materials[i] == null)
                    throw new InvalidConfigException($"Missing material for tile type \"{_tileTypes[i]}\".");
            }

            meshRenderer.sharedMaterials = materials;
        }

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            simController.OnStateReset += HandleStateReset;

            _terrainMeshFilter.mesh = GenerateTerrainMesh(simController.TerrainMap, simController.HeightMap);
            GenerateTerrainDetails(simController.TerrainMap, simController.HeightMap);
        }

        /// <summary>
        ///     Called on game object disabled.
        /// </summary>
        private void OnDisable()
        {
            simController.OnStateReset -= HandleStateReset;

            foreach (GameObject thing in _terrainDetails) Destroy(thing);
            _terrainDetails.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="InvalidConfigException"></exception>
        private void ValidateConfig()
        {
            if (treePrefabs.Length == 0 || buildingPrefabs.Length == 0) throw new InvalidConfigException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="terrainMap"></param>
        /// <param name="heightMap"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private Mesh GenerateTerrainMesh(Tile[,] terrainMap, float[,] heightMap)
        {
            int tileWidth = terrainMap.GetLength(0);
            int tileHeight = terrainMap.GetLength(1);

            int vertexWidth = heightMap.GetLength(0);
            int vertexHeight = heightMap.GetLength(1);

            if (vertexWidth != tileWidth + 1 || vertexHeight != tileHeight + 1)
                throw new InvalidOperationException();

            // Vertex & uv generation.
            var vertices = new Vector3[vertexWidth * vertexHeight];
            var uv = new Vector2[vertices.Length];

            for (var y = 0; y < vertexHeight; y++)
            for (var x = 0; x < vertexWidth; x++)
            {
                int vertexIndex = Index(x, y);

                vertices[vertexIndex] = new Vector3(
                    x * SimController.WorldScale,
                    heightMap[x, y],
                    y * SimController.WorldScale);

                uv[vertexIndex] = new Vector2(x, y);
            }

            // Triangle generation.
            var trianglesBySubmesh = new List<int>[_tileTypes.Length];
            for (var i = 0; i < trianglesBySubmesh.Length; i++)
                trianglesBySubmesh[i] = new List<int>();

            for (var y = 0; y < tileHeight; y++)
            for (var x = 0; x < tileWidth; x++)
            {
                List<int> triangles = trianglesBySubmesh[(int)terrainMap[x, y]];

                int lowerLeft = Index(x, y);
                int lowerRight = Index(x + 1, y);
                int upperLeft = Index(x, y + 1);
                int upperRight = Index(x + 1, y + 1);

                triangles.Add(lowerLeft);
                triangles.Add(upperLeft);
                triangles.Add(lowerRight);
                triangles.Add(upperLeft);
                triangles.Add(upperRight);
                triangles.Add(lowerRight);
            }

            // Create mesh.
            Mesh mesh = new();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = trianglesBySubmesh.Length;

            for (var i = 0; i < trianglesBySubmesh.Length; i++)
                mesh.SetTriangles(trianglesBySubmesh[i], i);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            int Index(int x, int y) => x + y * vertexWidth;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="terrainMap"></param>
        /// <param name="heightMap"></param>
        private void GenerateTerrainDetails(Tile[,] terrainMap, float[,] heightMap)
        {
            int width = terrainMap.GetLength(0);
            int height = terrainMap.GetLength(1);

            for (uint y = 0; y < height; y++)
            for (uint x = 0; x < width; x++)
            {
                // Instantiate prefab.
                Tile type = terrainMap[x, y];
                switch (type)
                {
                    case Tile.Woodland:
                        GenerateWoodlandDetails(heightMap, x, y);
                        continue;
                    case Tile.Building:
                        GenerateBuildingDetails(heightMap, x, y);
                        continue;
                    case Tile.Paved or Tile.Grassland:
                        continue;
                    default:
                        throw new ImpossibleStateException();
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        private void GenerateBuildingDetails(float[,] heightMap, uint xCoord, uint yCoord)
        {
            const int scale = SimController.WorldScale;
            GameObject prefab = buildingPrefabs[UnityEngine.Random.Range(0, buildingPrefabs.Length)];

            Vector3 position = new(
                xCoord * scale + scale * 0.5f,
                heightMap[xCoord, yCoord],
                yCoord * scale + scale * 0.5f);

            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 3) * 90f, 0f);

            GameObject detailObject = Instantiate(prefab, position, rotation, transform);
            _terrainDetails.Add(detailObject);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        private void GenerateWoodlandDetails(float[,] heightMap, uint xCoord, uint yCoord)
        {
            const int scale = SimController.WorldScale;
            int iterations = UnityEngine.Random.Range(3, 4);
            var objectPositions = new Vector3[iterations];

            for (var i = 0; i < iterations; i++)
            {
                GameObject prefab = treePrefabs[UnityEngine.Random.Range(0, treePrefabs.Length)];

                Vector3 position;
                bool isPositionValid;
                int attempts = 0;
                // do
                // {
                    position = new Vector3(
                        xCoord * scale + scale * UnityEngine.Random.Range(0.01f, 0.99f),
                        heightMap[xCoord, yCoord],
                        yCoord * scale + scale * UnityEngine.Random.Range(0.01f, 0.99f));
                    isPositionValid = true;
                //
                //     // Check that the random point is not too close to any existing positions.
                //     for (var vec = 0; vec < i; vec++)
                //     {
                //         if (position.x - objectPositions[vec].x < scale * 0.1 ||
                //             position.x - objectPositions[vec].x > scale * -0.1 ||
                //             position.y - objectPositions[vec].y < scale * 0.1 ||
                //             position.y - objectPositions[vec].y > scale * -0.1)
                //         {
                //             isPositionValid = false;
                //             attempts++;
                //             break;
                //         }
                //     }
                // } while (!isPositionValid || attempts > 5);

                Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);

                GameObject detailObject = Instantiate(prefab, position, rotation, transform);
                _terrainDetails.Add(detailObject);
                objectPositions[i] = position;
            }
        }

        /// <summary>
        /// Handle
        /// </summary>
        private void HandleStateReset()
        {
        }
    }
}