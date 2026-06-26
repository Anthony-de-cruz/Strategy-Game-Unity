using System;
using System.Buffers.Binary;
using System.Text;
using Newtonsoft.Json;

namespace GameLogic
{
    public static class MapLoader
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static (Tile[][], uint, uint, UnitSpawn[]) LoadFromJson(string jsonString)
        {
            var mapData = JsonConvert.DeserializeObject<MapData>(jsonString);
            if (mapData == null) throw new InvalidOperationException("Map JSON could not be deserialized.");

            var map = new Tile[mapData.Width][];
            for (var x = 0; x < mapData.Width; x++)
            {
                map[x] = new Tile[mapData.Height];
                for (var y = 0; y < mapData.Height; y++)
                    map[x][y] = new Tile(KeyToType(mapData.Tiles[y][x].ToString()), 0);
            }

            //Array.Reverse(map);

            UnitSpawn[] units = ReadUnits(mapData);

            return (map, mapData.Width, mapData.Height, units);
        }

        public static ushort[] LoadHeightMapFromRaw(ReadOnlySpan<byte> bytes, int width, int height)
        {
            int sampleCount = width * height;
            int expectedBytes = sampleCount * sizeof(ushort);
            if (bytes.Length != expectedBytes) throw new ImpossibleStateException();

            var samples = new ushort[sampleCount];
            // Read out every 16 bits into an ushort.
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = BinaryPrimitives.ReadUInt16LittleEndian(
                    bytes.Slice(i * sizeof(ushort), sizeof(ushort)));
            }

            return samples;
        }

        public static string MapToJson(Tile[][] map, UnitSpawn[] units)
        {
            MapData mapData = CreateMapData(map);
            WriteUnits(mapData, units);
            return JsonConvert.SerializeObject(
                mapData,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private static MapData CreateMapData(Tile[][] map)
        {
            var width = (uint)map.Length;
            var height = (uint)map[0].Length;

            var tiles = new string[height];
            for (var y = 0; y < height; y++)
            {
                var row = new StringBuilder();
                for (var x = 0; x < width; x++)
                    row.Append(TileToKey(map[x][y].Type));

                tiles[y] = row.ToString();
            }

            return new MapData
            {
                Width = width,
                Height = height,
                Tiles = tiles
            };
        }

        private static UnitSpawn[] ReadUnits(MapData mapData)
        {
            if (mapData.BlueUnits == null && mapData.RedUnits == null && mapData.Units == null) return null;

            int unitCount = (mapData.BlueUnits?.Length ?? 0) + (mapData.RedUnits?.Length ?? 0);
            if (mapData.Units != null) unitCount += mapData.Units.Length;

            var units = new UnitSpawn[unitCount];
            var index = 0;

            if (mapData.BlueUnits != null)
                index = ReadTeamUnits(mapData.BlueUnits, UnitTeam.Blue, units, index);

            if (mapData.RedUnits != null)
                index = ReadTeamUnits(mapData.RedUnits, UnitTeam.Red, units, index);

            if (mapData.Units == null) return units;

            foreach (UnitData unitData in mapData.Units)
            {
                units[index] = new UnitSpawn(
                    UnitTeamFromString(unitData.Team),
                    UnitTypeFromString(unitData.Type),
                    unitData.X,
                    unitData.Y
                );
                index++;
            }

            return units;
        }

        private static int ReadTeamUnits(UnitData[] unitData, UnitTeam team, UnitSpawn[] units, int startIndex)
        {
            var index = startIndex;
            for (var i = 0; i < unitData.Length; i++)
            {
                units[index] = new UnitSpawn(
                    team,
                    UnitTypeFromString(unitData[i].Type),
                    unitData[i].X,
                    unitData[i].Y
                );
                index++;
            }

            return index;
        }

        private static void WriteUnits(MapData mapData, UnitSpawn[] units)
        {
            if (units == null) return;

            var blueUnitCount = 0;
            var redUnitCount = 0;
            for (var i = 0; i < units.Length; i++)
                switch (units[i].Team)
                {
                    case UnitTeam.Blue:
                        blueUnitCount++;
                        break;
                    case UnitTeam.Red:
                        redUnitCount++;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

            mapData.BlueUnits = new UnitData[blueUnitCount];
            mapData.RedUnits = new UnitData[redUnitCount];

            var blueIndex = 0;
            var redIndex = 0;
            for (var i = 0; i < units.Length; i++)
            {
                UnitData unitData = new UnitData
                {
                    Type = units[i].Type.ToString(),
                    X = units[i].X,
                    Y = units[i].Y
                };

                switch (units[i].Team)
                {
                    case UnitTeam.Blue:
                        mapData.BlueUnits[blueIndex] = unitData;
                        blueIndex++;
                        break;
                    case UnitTeam.Red:
                        mapData.RedUnits[redIndex] = unitData;
                        redIndex++;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private static string TileToKey(TileType type)
        {
            return type switch
            {
                TileType.Paved => "P",
                TileType.Grassland => "G",
                TileType.Woodland => "W",
                TileType.Building => "B",
                _ => throw new InvalidOperationException()
            };
        }


        private static TileType KeyToType(string str)
        {
            return str switch
            {
                "P" => TileType.Paved,
                "G" => TileType.Grassland,
                "W" => TileType.Woodland,
                "B" => TileType.Building,
                _ => throw new InvalidOperationException()
            };
        }

        private static UnitTeam UnitTeamFromString(string str)
        {
            return str switch
            {
                nameof(UnitTeam.Blue) => UnitTeam.Blue,
                nameof(UnitTeam.Red) => UnitTeam.Red,
                _ => throw new InvalidOperationException()
            };
        }

        private static UnitType UnitTypeFromString(string str)
        {
            return str switch
            {
                nameof(UnitType.Infantry) => UnitType.Infantry,
                nameof(UnitType.Tank) => UnitType.Tank,
                _ => throw new InvalidOperationException()
            };
        }

        private class MapData
        {
            [JsonProperty("width")] public uint Width { get; set; }

            [JsonProperty("height")] public uint Height { get; set; }

            [JsonProperty("tiles")] public string[] Tiles { get; set; }

            [JsonProperty("blueUnits", NullValueHandling = NullValueHandling.Ignore)]
            public UnitData[] BlueUnits { get; set; }

            [JsonProperty("redUnits", NullValueHandling = NullValueHandling.Ignore)]
            public UnitData[] RedUnits { get; set; }

            [JsonProperty("units", NullValueHandling = NullValueHandling.Ignore)]
            public UnitData[] Units { get; set; }
        }

        private class UnitData
        {
            [JsonProperty("team", NullValueHandling = NullValueHandling.Ignore)]
            public string Team { get; set; }

            [JsonProperty("type")] public string Type { get; set; }

            [JsonProperty("x")] public uint X { get; set; }

            [JsonProperty("y")] public uint Y { get; set; }
        }

        /// <summary>
        ///
        /// </summary>
        public readonly struct UnitSpawn
        {
            public readonly UnitTeam Team;
            public readonly UnitType Type;
            public readonly uint X;
            public readonly uint Y;

            /// <summary>
            ///     Constructor for <see cref="UnitSpawn"/>.
            /// </summary>
            /// <param name="team"></param>
            /// <param name="type"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public UnitSpawn(UnitTeam team, UnitType type, uint x, uint y)
            {
                Team = team;
                Type = type;
                X = x;
                Y = y;
            }
        }
    }
}