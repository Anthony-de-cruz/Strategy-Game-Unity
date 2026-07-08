using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Simulation
{
    public static class MapLoader
    {
        /// <summary>
        /// The maximum map name length.
        /// </summary>
        private const int MaxMapNameLength = 256;

        /// <summary>
        /// The maximum total map tile count.
        /// </summary>
        private const int MaxMapSize = 5000;

        /// <summary>
        /// Load map metadata from a JSON style string.
        /// </summary>
        /// <param name="json">The JSON style string.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Invalid map data.</exception>
        public static (string, uint, uint, UnitData[]) LoadMetaFromJson(string json)
        {
#nullable enable
            JsonMapData? jsonMapData;
            try
            {
                jsonMapData = JsonConvert.DeserializeObject<JsonMapData>(json);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to deserialize map data", e);
            }

            if (jsonMapData is null)
                throw new InvalidOperationException("Failed to deserialize map data");
#nullable disable

            if (jsonMapData.Name.Length == 0 ||
                jsonMapData.Name.Length > MaxMapNameLength)
                throw new InvalidOperationException(
                    $"Invalid map name: {jsonMapData.Name}. " +
                    $"Expected length {MaxMapNameLength} but found {jsonMapData.Name.Length}");

            if (jsonMapData.Width * jsonMapData.Height > MaxMapSize)
                throw new InvalidOperationException(
                    $"Invalid map size: {jsonMapData.Width},{jsonMapData.Height}. " +
                    $"Exceeds {MaxMapSize} but found {jsonMapData.Width * jsonMapData.Height}");

            var units = new UnitData[jsonMapData.Units.Length];
            for (var i = 0; i < units.Length; i++)
                units[i] = new UnitData(
                    jsonMapData.Units[i].Team switch
                    {
                        nameof(UnitTeam.Blue) => UnitTeam.Blue,
                        nameof(UnitTeam.Red) => UnitTeam.Red,
                        _ => throw new InvalidOperationException($"Unhandled team type: {jsonMapData.Units[i].Team}")
                    },
                    jsonMapData.Units[i].Type switch
                    {
                        nameof(UnitType.Infantry) => UnitType.Infantry,
                        nameof(UnitType.Tank) => UnitType.Tank,
                        _ => throw new InvalidOperationException($"Unhandled unit type: {jsonMapData.Units[i].Type}")
                    },
                    jsonMapData.Units[i].X,
                    jsonMapData.Units[i].Y);

            return (jsonMapData.Name, jsonMapData.Width, jsonMapData.Height, units);
        }

        /// <summary>
        /// Generate a terrain map from 32-bit RGBA RAW image bytes.
        /// </summary>
        /// <param name="bytes">Height map encoded as 32-bit RGBA RAW.</param>
        /// <param name="width">Map width.</param>
        /// <param name="height">Map height.</param>
        /// <returns>[x,y] 2d array terrain map.</returns>
        /// <exception cref="InvalidOperationException">Length of <paramref name="bytes"/> does not match expected size or a pixel is not recognized value.</exception>
        public static Tile[,] LoadTerrainMapFromRaw(ReadOnlySpan<byte> bytes, uint width, uint height)
        {
            uint expectedBytes = width * height * 4;
            if (bytes.Length != expectedBytes)
                throw new InvalidOperationException($"Expected {expectedBytes} bytes, got {bytes.Length}");

            var tiles = new Tile[width, height];

            for (uint y = 0; y < height; y++)
            for (uint x = 0; x < width; x++)
            {
                uint offset = (x + y * width) * 4;

                byte r = bytes[(int)offset];
                byte g = bytes[(int)offset + 1];
                byte b = bytes[(int)offset + 2];
                byte a = bytes[(int)offset + 3];
                if (a != 255)
                    throw new InvalidOperationException(
                        $"Terrain pixel at ({x},{y}) is not opaque RGBA: ({r},{g},{b},{a})");
                if (r == 64 && g == 64 && b == 32)
                    tiles[x, y] = Tile.Paved;
                else if (r == 255 && g == 160 && b == 255)
                    tiles[x, y] = Tile.Building;
                else if (r == 0 && g == 245 && b == 0)
                    tiles[x, y] = Tile.Grassland;
                else if (r == 0 && g == 165 && b == 0)
                    tiles[x, y] = Tile.Woodland;
                else throw new InvalidOperationException($"Invalid terrain type at ({x},{y}): ({r},{g},{b},{a})");
            }

            return tiles;
        }

        /// <summary>
        /// Generate a normalized heightmap from 32-bit RGBA greyscale RAW image bytes.
        /// </summary>
        /// <param name="bytes">Height map encoded as 32-bit RGBA greyscale RAW.</param>
        /// <param name="width">Map width.</param>
        /// <param name="height">Map height.</param>
        /// <param name="scale">Scale multiplier for normalized height map.</param>
        /// <returns>[x,y] 2d array height map.</returns>
        /// <exception cref="InvalidOperationException">Length of <paramref name="bytes"/> does not match expected size or a pixel is not opaque greyscale RGBA.</exception>
        public static float[,] LoadHeightMapFromRaw(ReadOnlySpan<byte> bytes, uint width, uint height, int scale)
        {
            // // For every 16-bit integer greyscale pixel, map to 2d array and normalize to map scale.
            // // ushort / 65535f -> 0.0 to 1.0
            // var samples = new uint[width, height];
            // for (uint y = 0; y < height; y++)
            // for (uint x = 0; x < width; x++)
            //     samples[x, y] = (uint)(BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(
            //         (int)(x + y * width) * sizeof(ushort),
            //         sizeof(ushort))));
            //     //) / 65535f * scale);

            uint expectedBytes = width * height * 4;
            if (bytes.Length != expectedBytes)
                throw new InvalidOperationException($"Expected {expectedBytes} bytes, got {bytes.Length}");

            var tiles = new float[width, height];

            for (uint y = 0; y < height; y++)
            for (uint x = 0; x < width; x++)
            {
                uint offset = (x + y * width) * 4;

                byte r = bytes[(int)offset];
                byte g = bytes[(int)offset + 1];
                byte b = bytes[(int)offset + 2];
                byte a = bytes[(int)offset + 3];
                if (r != g || r != b || a != 255)
                    throw new InvalidOperationException(
                        $"Height pixel at ({x},{y}) is not opaque greyscale RGBA: ({r},{g},{b},{a})");

                tiles[x, y] = (float)(r * scale) / 255;
            }

            return ConvertHeightMapTileToVertex(tiles);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="heightMap"></param>
        /// <returns></returns>
        private static float[,] ConvertHeightMapTileToVertex(float[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            var vertices = new float[width + 1, height + 1];

            // Set each vertex to the average height of each adjacent tile.
            for (var vy = 0; vy < height + 1; vy++)
            for (var vx = 0; vx < width + 1; vx++)
            {
                var sum = 0f;
                var count = 0;
                // Iterate over the bottom left to the top right adjacent tiles.
                for (int ty = vy - 1; ty <= vy; ty++)
                for (int tx = vx - 1; tx <= vx; tx++)
                {
                    if (tx < 0 || ty < 0 || tx >= width || ty >= height) continue;
                    sum += heightMap[tx, ty];
                    count++;
                }

                vertices[vx, vy] = sum / count;
            }

            return vertices;
        }

        public static string SaveMapToJson(string name, uint width, uint height, List<UnitData> units)
        {
            throw new NotImplementedException();
            //return JsonConvert.SerializeObject();
        }

        /// <summary>
        /// Map data for JSON serialization
        /// </summary>
        private class JsonMapData
        {
            [JsonProperty("name", Required = Required.Always)]
            public string Name { get; set; }

            [JsonProperty("width", Required = Required.Always)]
            public uint Width { get; set; }

            [JsonProperty("height", Required = Required.Always)]
            public uint Height { get; set; }

            [JsonProperty("units", Required = Required.DisallowNull)]
            public JsonUnitData[] Units { get; set; }
        }

        /// <summary>
        /// Unit data for JSON serialization.
        /// </summary>
        private class JsonUnitData
        {
            [JsonProperty("team", Required = Required.Always)]
            public string Team { get; set; }

            [JsonProperty("type", Required = Required.Always)]
            public string Type { get; set; }

            [JsonProperty("x", Required = Required.Always)]
            public uint X { get; set; }

            [JsonProperty("y", Required = Required.Always)]
            public uint Y { get; set; }
        }

        /// <summary>
        ///
        /// </summary>
        public readonly struct UnitData
        {
            public readonly UnitTeam Team;
            public readonly UnitType Type;
            public readonly uint X;
            public readonly uint Y;

            /// <summary>
            ///     Constructor for <see cref="UnitData"/>.
            /// </summary>
            /// <param name="team"></param>
            /// <param name="type"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public UnitData(UnitTeam team, UnitType type, uint x, uint y)
            {
                Team = team;
                Type = type;
                X = x;
                Y = y;
            }
        }
    }
}