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

        private readonly TileType[] _tileTypes =
            Enum.GetValues(typeof(TileType))
                .Cast<TileType>()
                .ToArray();

        private MeshFilter _terrainMeshFilter;
        private List<GameObject> _terrainDetails = new();

        /// <summary>
        ///     Type for configuration.
        /// </summary>
        [Serializable]
        public struct TileMaterial
        {
            public TileType type;
            public Material material;
        }

        /// <summary>
        ///     Temporary.
        /// </summary>
        private struct TerrainTile
        {
            public readonly float Height;
            public readonly TileType Type;

            public TerrainTile(float height, TileType type)
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
            var map = new TerrainTile[,]
            {
                {
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(1, TileType.Woodland),
                    new(1, TileType.Woodland),
                    new(1, TileType.Paved),
                    new(1, TileType.Paved),
                    new(1, TileType.Grassland),
                    new(0, TileType.Grassland)
                },
                {
                    new(0, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(2, TileType.Woodland),
                    new(5, TileType.Woodland),
                    new(5, TileType.Building),
                    new(3, TileType.Building),
                    new(1, TileType.Paved),
                    new(0, TileType.Grassland)
                },
                {
                    new(0, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Woodland),
                    new(5, TileType.Woodland),
                    new(5, TileType.Paved),
                    new(2, TileType.Paved),
                    new(1, TileType.Paved),
                    new(0, TileType.Grassland)
                },
                {
                    new(1, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Paved),
                    new(1, TileType.Paved),
                    new(1, TileType.Paved),
                    new(1, TileType.Paved),
                    new(1, TileType.Grassland)
                },
                {
                    new(0, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Woodland),
                    new(1, TileType.Woodland),
                    new(1, TileType.Paved),
                    new(3, TileType.Building),
                    new(1, TileType.Paved),
                    new(0, TileType.Grassland)
                },
                {
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(1, TileType.Woodland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Paved),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland)
                },
                {
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(1, TileType.Woodland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland)
                },
                {
                    new(0, TileType.Grassland),
                    new(1, TileType.Paved),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(1, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland)
                },
                {
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland),
                    new(0, TileType.Grassland)
                },
            };

            _terrainMeshFilter.mesh = GenerateTerrainMesh(map);
            GenerateTerrainDetails(map);

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
        /// <param name="heightMap"></param>
        private Mesh GenerateTerrainMesh(TerrainTile[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            int scale = SimController.WorldScale;

            // Vertex & uv generation.
            var vertices = new Vector3[width * height];
            var uv = new Vector2[vertices.Length];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    int vertexIndex = Index(x, y);
                    vertices[vertexIndex] = new Vector3(x * scale, heightMap[x, y].Height, y * scale);
                    uv[vertexIndex] = new Vector2(x, y);
                }
            }

            // Triangle generation.
            var trianglesBySubmesh = new List<int>[_tileTypes.Length];
            for (var i = 0; i < trianglesBySubmesh.Length; i++)
                trianglesBySubmesh[i] = new List<int>();

            for (var y = 0; y < height - 1; y++)
            {
                for (var x = 0; x < width - 1; x++)
                {
                    List<int> triangles = trianglesBySubmesh[(int)heightMap[x, y].Type];

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

            int Index(int x, int y) => x + y * width;
        }

        private void GenerateTerrainDetails(TerrainTile[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            int scale = SimController.WorldScale;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // Instantiate prefab.
                    if (heightMap[x, y].Type is TileType.Grassland or TileType.Paved)
                        continue;
                    GameObject prefab = heightMap[x, y].Type is TileType.Woodland
                        ? prefabTrees
                        : prefabBuilding;
                    Vector3 position = new(
                        x * scale + scale * 0.5f,
                        heightMap[x, y].Height,
                        y * scale + scale * 0.5f
                    );
                    GameObject detailObject = Instantiate(
                        prefab,
                        position,
                        // Random rotation.
                        Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90f, 0f),
                        transform
                    );
                    _terrainDetails.Add(detailObject);
                }
            }
        }

        private void HandleStateReset()
        {
        }
    }
}