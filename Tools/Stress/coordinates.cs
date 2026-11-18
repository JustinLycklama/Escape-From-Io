// Play Mode check (#11, #1): round-trip every layout tile through the coordinate spaces and report mismatches.
//   Layout -> MapCoordinate -> WorldPosition -> MapCoordinate.FromWorldPosition -> Layout
//   Layout -> MapCoordinate -> PathGridCoordinate -> MapCoordinate.FromGridCoordinate -> Layout
// Run after GameScene initializes: unity command script eval --file Tools/Stress/coordinates.cs
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Linq;
using System.Text;
using System.Collections.Generic;

var constants = UnityEngine.Object.FindAnyObjectByType<Constants>();
var maps = UnityEngine.Object.FindAnyObjectByType<MapsManager>();
var sb = new StringBuilder();
int checkedTiles = 0, worldFails = 0, gridFails = 0;
var samples = new List<string>();
var worldTargets = new Dictionary<Vector3, int>();

foreach (var container in maps.mapContainers) {
    for (int x = 0; x < constants.layoutMapWidth; x++) {
        for (int y = 0; y < constants.layoutMapHeight; y++) {
            var layout = new LayoutCoordinate(x, y, container);
            var map = new MapCoordinate(layout);
            var world = new WorldPosition(map);
            checkedTiles++;

            var key = new Vector3(Mathf.Round(world.vector3.x), 0, Mathf.Round(world.vector3.z));
            worldTargets[key] = worldTargets.TryGetValue(key, out var n) ? n + 1 : 1;

            var backFromWorld = new LayoutCoordinate(MapCoordinate.FromWorldPosition(world));
            if (backFromWorld != layout) {
                worldFails++;
                if (samples.Count < 6) samples.Add($"world: map({container.mapX},{container.mapY}) tile({x},{y}) -> {world.vector3} -> map({backFromWorld.mapContainer.mapX},{backFromWorld.mapContainer.mapY}) tile({backFromWorld.x},{backFromWorld.y})");
            }

            var grid = PathGridCoordinate.fromMapCoordinate(map);
            var backFromGrid = new LayoutCoordinate(MapCoordinate.FromGridCoordinate(grid));
            if (backFromGrid != layout) {
                gridFails++;
                if (samples.Count < 12) samples.Add($"grid: map({container.mapX},{container.mapY}) tile({x},{y}) -> grid({grid.xLowSample},{grid.yLowSample}) -> map({backFromGrid.mapContainer.mapX},{backFromGrid.mapContainer.mapY}) tile({backFromGrid.x},{backFromGrid.y})");
            }
        }
    }
}

int sharedTargets = worldTargets.Count(kv => kv.Value > 1);
sb.AppendLine($"maps={maps.mapContainers.Count()} tiles={checkedTiles} worldRoundTripFails={worldFails} gridRoundTripFails={gridFails} tilesSharingAWorldPoint={sharedTargets}");
foreach (var s in samples) sb.AppendLine("  " + s);
return sb.ToString();
