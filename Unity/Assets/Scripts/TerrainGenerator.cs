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
        [SerializeField] private GameObject prefabBuilding;
        [SerializeField] private GameObject prefabTrees;

        private readonly Tile[] _tileTypes =
            Enum.GetValues(typeof(Tile))
                .Cast<Tile>()
                .ToArray();

        private MeshFilter _terrainMeshFilter;
        private List<GameObject> _terrainDetails = new();

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
        ///     Temporary.
        /// </summary>
        private struct TerrainTile
        {
            public readonly float Height;
            public readonly Tile Type;

            public TerrainTile(float height, Tile type)
            {
                Height = height;
                Type = type;
            }
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
            _terrainMeshFilter.mesh = GenerateTerrainMesh(simController.TerrainMap, simController.HeightMap);
            //GenerateTerrainDetails(map);

            simController.OnStateReset += HandleStateReset;
        }

        /// <summary>
        ///     Called on game object disabled.
        /// </summary>
        private void OnDisable()
        {
            simController.OnStateReset -= HandleStateReset;
            foreach (GameObject thing in _terrainDetails) Destroy(thing);
        }

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="InvalidConfigException"></exception>
        private void ValidateConfig()
        {
            if (prefabBuilding == null || prefabTrees == null) throw new InvalidConfigException();
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

        // private void GenerateTerrainDetails(Tile[,] terrainMap, float[,] heightMap)
        // {
        //     int width = heightMap.GetLength(0);
        //     int height = heightMap.GetLength(1);
        //     int scale = SimController.WorldScale;
        //
        //     for (var y = 0; y < height; y++)
        //     {
        //         for (var x = 0; x < width; x++)
        //         {
        //             // Instantiate prefab.
        //             if (heightMap[x, y].Type is Tile.Grassland or Tile.Paved)
        //                 continue;
        //             GameObject prefab = heightMap[x, y].Type is Tile.Woodland
        //                 ? prefabTrees
        //                 : prefabBuilding;
        //             Vector3 position = new(
        //                 x * scale + scale * 0.5f,
        //                 heightMap[x, y].Height,
        //                 y * scale + scale * 0.5f
        //             );
        //             GameObject detailObject = Instantiate(
        //                 prefab,
        //                 position,
        //                 // Random rotation.
        //                 Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90f, 0f),
        //                 transform
        //             );
        //             _terrainDetails.Add(detailObject);
        //         }
        //     }
        // }

        private void HandleStateReset()
        {
        }
    }
}