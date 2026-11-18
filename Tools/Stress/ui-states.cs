// Play Mode helper for UI review: put the HUD into a given state. Edit STATE below, or copy per state.
//   STATE = "terrain": select a tile next to spawn (shows the actions panel)
//   STATE = "unit":    select the first robot (shows unit detail panel)
//   STATE = "clear":   remove selection
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;

string STATE = System.Environment.GetEnvironmentVariable("UI_STATE") ?? "terrain";
var selection = UnityEngine.Object.FindAnyObjectByType<SelectionManager>();
var mapGen = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
var constants = UnityEngine.Object.FindAnyObjectByType<Constants>();

switch (STATE) {
    case "terrain": {
        var s = mapGen.spawnCoordinate;
        int x = Mathf.Clamp(s.x + 1, 0, constants.layoutMapWidth - 1);
        selection.SelectTerrain(new LayoutCoordinate(x, s.y, s.mapContainer));
        return "selected terrain";
    }
    case "unit": {
        var unit = UnityEngine.Object.FindAnyObjectByType<UnitManager>().GetAllPlayerUnits()[0];
        selection.SelectSelectable(unit);
        return "selected " + unit.title;
    }
    default:
        selection.RemoveSelection();
        return "cleared";
}
