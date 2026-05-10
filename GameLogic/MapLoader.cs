using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GameLogic
{
    public static class MapLoader
    {
        public static (Tile[][], uint, uint) LoadFromJson(string jsonString)
        {
            MapData mapData = Deserialize(jsonString);

            var map = new Tile[mapData.Width][];
            for (var x = 0; x < mapData.Width; x++)
            {
                map[x] = new Tile[mapData.Height];
                for (var y = 0; y < mapData.Height; y++)
                    map[x][y] = new Tile(KeyToType(mapData.Tiles[y][x].ToString()), 0);
            }

            //Array.Reverse(map);

            return (map, mapData.Width, mapData.Height);
        }

        public static string TileMapToJson(Tile[][] map)
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

            var mapData = new MapData
            {
                Width = width,
                Height = height,
                Tiles = tiles
            };

            return Serialize(mapData);
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

        private static MapData Deserialize(string jsonString)
        {
            var serialiser = new DataContractJsonSerializer(typeof(MapData),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            return (MapData)serialiser.ReadObject(stream);
        }

        private static string Serialize(MapData mapData)
        {
            var serialiser = new DataContractJsonSerializer(typeof(MapData),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            using var stream = new MemoryStream();
            serialiser.WriteObject(stream, mapData);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        [DataContract]
        private class MapData
        {
            [DataMember(Name = "width")] public uint Width { get; set; }
            [DataMember(Name = "height")] public uint Height { get; set; }
            [DataMember(Name = "tiles")] public string[] Tiles { get; set; }
        }
    }
}