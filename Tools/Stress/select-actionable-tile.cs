// Play Mode: select the nearest tile to spawn that offers player actions (to review the command card's action row).
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Linq;

var selection = UnityEngine.Object.FindAnyObjectByType<SelectionManager>();
var mapGen = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
var terrain = UnityEngine.Object.FindAnyObjectByType<TerrainManager>();
var constants = UnityEngine.Object.FindAnyObjectByType<Constants>();
var s = mapGen.spawnCoordinate;

for (int r = 1; r <= 4; r++) {
    for (int dx = -r; dx <= r; dx++) {
        for (int dy = -r; dy <= r; dy++) {
            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
            int x = s.x + dx, y = s.y + dy;
            if (x < 0 || y < 0 || x >= constants.layoutMapWidth || y >= constants.layoutMapHeight) continue;
            var c = new LayoutCoordinate(x, y, s.mapContainer);
            var actions = terrain.ActionsAvailableForTerrain(c);
            if (actions != null && actions.Length > 0) {
                selection.SelectTerrain(c);
                return $"selected tile ({x},{y}) '{c.mapContainer.map.GetTerrainAt(c).name}' with {actions.Length} action(s): {string.Join(", ", actions.Select(a => a.description))}";
            }
        }
    }
}
return "no actionable tile near spawn";
