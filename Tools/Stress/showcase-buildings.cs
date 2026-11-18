// Play Mode (#6): place a Shuttle Frame and a Sensor Tower beside spawn, complete them instantly, show the sensor arrow,
// and pan the camera there for a screenshot. Run after GameScene initializes.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Reflection;
using System.Text;
using System.Collections;

var mapGen = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
var constants = UnityEngine.Object.FindAnyObjectByType<Constants>();
var player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
var spawn = mapGen.spawnCoordinate;
var sb = new StringBuilder();

LayoutCoordinate Near(int dx, int dy) {
    int x = Mathf.Clamp(spawn.x + dx, 0, constants.layoutMapWidth - 1);
    int y = Mathf.Clamp(spawn.y + dy, 0, constants.layoutMapHeight - 1);
    return new LayoutCoordinate(x, y, spawn.mapContainer);
}

Building Place(Building.Blueprint bp, LayoutCoordinate c) {
    var b = bp.ConstructAt(c).GetComponent<Building>();
    sb.AppendLine($"{bp.label} at tile ({c.x},{c.y}) world {new WorldPosition(new MapCoordinate(c)).vector3}");
    return b;
}

// Complete after a couple of frames: buildings cache references in Start, which runs the frame after instantiation
IEnumerator CompleteLater(Building[] buildings) {
    yield return null; yield return null;
    foreach (var b in buildings) {
        b.performAction(new GameTask("showcase", new WorldPosition(b.transform.position), GameTask.ActionType.Build, b), 1000f, null);
    }
    yield return null;
    var sensor = buildings[1] as SensorTower;
    var arrow = typeof(SensorTower).GetField("arrow", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(sensor) as GameObject;
    if (arrow != null) arrow.SetActive(true);
}

var frame = Place(Building.Blueprint.StationShipFrame, Near(2, 0));
var sensorTower = Place(Building.Blueprint.SensorTower, Near(1, 1));
player.StartCoroutine(CompleteLater(new[] { frame, sensorTower }));

var mid = (frame.transform.position + sensorTower.transform.position) / 2f;
player.PanCameraToPosition(mid, 0f, false);
return sb.ToString();
