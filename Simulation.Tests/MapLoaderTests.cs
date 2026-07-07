using Simulation;

namespace Simulation.Tests;

// public class MapLoaderTests
// {
//     [Theory]
//     [InlineData("map0.json")]
//     [InlineData("map1.json")]
//     public void LoadFromJson_LoadsStreamingAssetMaps(string fileName)
//     {
//         string mapsDirectory = FindMapsDirectory();
//         string json = File.ReadAllText(Path.Combine(mapsDirectory, fileName));
//
//         (Tile[][] loadedMap, uint width, uint height, MapLoader.UnitData[] loadedUnits) =
//             MapLoader.LoadFromJson(json);
//
//         Assert.Equal(25u, width);
//         Assert.Equal(25u, height);
//         Assert.Equal((int)width, loadedMap.Length);
//         Assert.All(loadedMap, column => Assert.Equal((int)height, column.Length));
//         Assert.NotNull(loadedUnits);
//         Assert.NotEmpty(loadedUnits);
//     }
//
//     [Fact]
//     public void TileMapToJson_RoundTripsUnits()
//     {
//         var map = new[]
//         {
//             new[] { new Tile(TileType.Grassland, 0), new Tile(TileType.Paved, 0) },
//             new[] { new Tile(TileType.Woodland, 0), new Tile(TileType.Building, 0) }
//         };
//         var units = new[]
//         {
//             new MapLoader.UnitData(UnitTeam.Blue, UnitType.Infantry, 0, 1),
//             new MapLoader.UnitData(UnitTeam.Red, UnitType.Tank, 1, 0)
//         };
//
//         string json = MapLoader.MapToJson(map, units);
//         (Tile[][] loadedMap, uint width, uint height, MapLoader.UnitData[] loadedUnits) =
//             MapLoader.LoadFromJson(json);
//
//         Assert.Equal(2u, width);
//         Assert.Equal(2u, height);
//         Assert.Equal(TileType.Grassland, loadedMap[0][0].Type);
//         Assert.Equal(TileType.Paved, loadedMap[0][1].Type);
//         Assert.Equal(TileType.Woodland, loadedMap[1][0].Type);
//         Assert.Equal(TileType.Building, loadedMap[1][1].Type);
//
//         Assert.Equal(2, loadedUnits.Length);
//         Assert.Equal(UnitTeam.Blue, loadedUnits[0].Team);
//         Assert.Equal(UnitType.Infantry, loadedUnits[0].Type);
//         Assert.Equal(0u, loadedUnits[0].X);
//         Assert.Equal(1u, loadedUnits[0].Y);
//         Assert.Equal(UnitTeam.Red, loadedUnits[1].Team);
//         Assert.Equal(UnitType.Tank, loadedUnits[1].Type);
//         Assert.Equal(1u, loadedUnits[1].X);
//         Assert.Equal(0u, loadedUnits[1].Y);
//     }
//
//     private static string FindMapsDirectory()
//     {
//         DirectoryInfo? directory = new(AppContext.BaseDirectory);
//         while (directory != null)
//         {
//             string candidate = Path.Combine(
//                 directory.FullName,
//                 "Unity",
//                 "Assets",
//                 "StreamingAssets",
//                 "Maps");
//             if (Directory.Exists(candidate)) return candidate;
//
//             directory = directory.Parent;
//         }
//
//         throw new DirectoryNotFoundException("Could not find Unity/Assets/StreamingAssets/Maps.");
//     }
// }
