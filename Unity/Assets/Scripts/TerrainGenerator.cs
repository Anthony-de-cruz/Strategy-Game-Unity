using System;
using GameLogic;
using UnityEngine;

namespace Assets.Scripts
{
    public class TerrainGenerator : MonoBehaviour
    {
        public SimController simController;

        public Material mat;

        /// <summary>
        ///
        /// </summary>
        [Serializable]
        public struct TileMaterial
        {
            public TileType type;
            public Material material;
        }

        public TileMaterial[] tileMaterials;

        private MeshFilter _meshFilter;

        /// <summary>
        ///     Called on script load.
        /// </summary>
        private void Awake()
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            var mesh = gameObject.AddComponent<MeshRenderer>();
            mesh.sharedMaterial = mat; // new Material(Shader.Find("TerrainGrass"));
            mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        ///     Called on game object enabled.
        /// </summary>
        private void OnEnable()
        {
            _meshFilter.mesh = GenerateMesh(new float[,]
            {
                { 0, 0, 1, 1, 1, 1, 1, 0 },
                { 0, 0, 1, 2, 5, 3, 1, 0 },
                { 0, 1, 1, 1, 2, 2, 1, 0 },
                { 1, 1, 1, 1, 1, 1, 1, 1 },
                { 0, 1, 1, 1, 1, 3, 1, 0 },
                { 0, 0, 1, 1, 1, 1, 0, 0 },
                { 0, 0, 1, 1, 1, 1, 0, 0 },
                { 0, 1, 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0 },
            });

            simController.OnStateReset += HandleStateReset;
        }

        /// <summary>
        ///     Called on game object disabled.
        /// </summary>
        private void OnDisable()
        {
            simController.OnStateReset -= HandleStateReset;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="heightMap"></param>
        private static Mesh GenerateMesh(float[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            int scale = SimController.WorldScale;

            var vertices = new Vector3[width * height];
            var uv = new Vector2[vertices.Length];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    int vertexIndex = Index(x, y);
                    vertices[vertexIndex] = new Vector3(x * scale, heightMap[x, y] * 2, y * scale);
                    uv[vertexIndex] = new Vector2(
                        width == 1 ? 0f : (float)x / (width - 1),
                        height == 1 ? 0f : (float)y / (height - 1)
                    );
                }
            }

            var tris = new int[(width - 1) * (height - 1) * 6];
            var triIndex = 0;
            for (var y = 0; y < height - 1; y++)
            {
                for (var x = 0; x < width - 1; x++)
                {
                    int lowerLeft = Index(x, y);
                    int lowerRight = Index(x + 1, y);
                    int upperLeft = Index(x, y + 1);
                    int upperRight = Index(x + 1, y + 1);

                    tris[triIndex++] = lowerLeft;
                    tris[triIndex++] = upperLeft;
                    tris[triIndex++] = lowerRight;
                    tris[triIndex++] = upperLeft;
                    tris[triIndex++] = upperRight;
                    tris[triIndex++] = lowerRight;
                }
            }

            Mesh mesh = new();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            // Map cartesian coords to nd-array index.
            int Index(int x, int y) => x + y * width;
        }

        private void HandleStateReset()
        {

        }
    }
}