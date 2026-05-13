using GameLogic;

namespace GameLogicTests;

public class MapLoaderTests
{
    [Fact]
    public void TileMapToJson_RoundTripsUnits()
    {
        var map = new[]
        {
            new[] { new Tile(TileType.Grassland, 0), new Tile(TileType.Paved, 0) },
            new[] { new Tile(TileType.Woodland, 0), new Tile(TileType.Building, 0) }
        };
        var units = new[]
        {
            new MapLoader.UnitSpawn(UnitTeam.Blue, UnitType.Infantry, 0, 1),
            new MapLoader.UnitSpawn(UnitTeam.Red, UnitType.Tank, 1, 0)
        };

        string json = MapLoader.TileMapToJson(map, units);
        (Tile[][] loadedMap, uint width, uint height, MapLoader.UnitSpawn[] loadedUnits) =
            MapLoader.LoadFromJson(json);

        Assert.Equal(2u, width);
        Assert.Equal(2u, height);
        Assert.Equal(TileType.Grassland, loadedMap[0][0].Type);
        Assert.Equal(TileType.Paved, loadedMap[0][1].Type);
        Assert.Equal(TileType.Woodland, loadedMap[1][0].Type);
        Assert.Equal(TileType.Building, loadedMap[1][1].Type);

        Assert.Equal(2, loadedUnits.Length);
        Assert.Equal(UnitTeam.Blue, loadedUnits[0].Team);
        Assert.Equal(UnitType.Infantry, loadedUnits[0].Type);
        Assert.Equal(0u, loadedUnits[0].X);
        Assert.Equal(1u, loadedUnits[0].Y);
        Assert.Equal(UnitTeam.Red, loadedUnits[1].Team);
        Assert.Equal(UnitType.Tank, loadedUnits[1].Type);
        Assert.Equal(1u, loadedUnits[1].X);
        Assert.Equal(0u, loadedUnits[1].Y);
    }
}
