// Stress tick: cancel a random in-progress player task (simulates the player cancelling mid-work), then report health.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

var unitManager = UnityEngine.Object.FindAnyObjectByType<UnitManager>();
var units = unitManager.GetAllPlayerUnits();

// Keep a workforce: robots run out of battery, so spawn replacements the way Narrator spawns starting units.
string spawned = "";
if (units.Length < 6) {
    var mapGen = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
    var blueprints = new[] { Unit.Blueprint.Miner, Unit.Blueprint.Builder, Unit.Blueprint.Mover };
    var bp = blueprints[UnityEngine.Random.Range(0, blueprints.Length)];
    var unit = UnityEngine.Object.Instantiate(bp.resource) as Unit;
    unit.transform.SetParent(unitManager.transform);
    var grid = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(mapGen.spawnCoordinate);
    var pos = new WorldPosition(MapCoordinate.FromGridCoordinate(grid[UnityEngine.Random.Range(0, 3)][UnityEngine.Random.Range(0, 3)]));
    pos.y = 15f;
    unit.transform.position = pos.vector3;
    unit.buildableComponent.allowFullTransparent = true;
    unit.buildableComponent.performAction(new GameTask("stress spawn", pos, GameTask.ActionType.Build, unit.buildableComponent), 100f, null);
    spawned = $" spawned={bp.label}";
}
var queue = UnityEngine.Object.FindAnyObjectByType<TaskQueueManager>();

// Keep the queues fed: a few new player-style terrain jobs each tick, and sometimes a Light Tower
// (tower = gather -> haul -> build chain with blocker tasks, the most complex task path).
{
    var constants = UnityEngine.Object.FindAnyObjectByType<Constants>();
    var mapGen = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
    var terrain = UnityEngine.Object.FindAnyObjectByType<TerrainManager>();
    var buildings = UnityEngine.Object.FindAnyObjectByType<BuildingManager>();
    var spawn = mapGen.spawnCoordinate;
    int added = 0, attempts = 0;
    bool wantTower = UnityEngine.Random.value < 0.3f;
    while ((added < 3 || wantTower) && attempts++ < 60) {
        int x = spawn.x + UnityEngine.Random.Range(-10, 11), y = spawn.y + UnityEngine.Random.Range(-10, 11);
        if (x < 0 || y < 0 || x >= constants.layoutMapWidth || y >= constants.layoutMapHeight) continue;
        var c = new LayoutCoordinate(x, y, spawn.mapContainer);
        if (buildings.buildingAtLocation(c) != null) continue;
        if (wantTower && mapGen.GetTerrainAt(c).buildable && Building.Blueprint.Tower.requirementsMet(c)) {
            Building.Blueprint.Tower.ConstructAt(c); wantTower = false; continue;
        }
        var act = terrain.ActionsAvailableForTerrain(c)?.FirstOrDefault(a =>
            a.tutorialIdentifier == UserAction.UserActionTutorialIdentifier.Mine ||
            a.tutorialIdentifier == UserAction.UserActionTutorialIdentifier.Clean);
        if (act != null && added < 3) { act.performAction(c); added++; }
    }
}
units = unitManager.GetAllPlayerUnits();
var busy = units.Where(u => u != null && u.currentMasterTask != null).ToArray();

string cancelled = "none";
if (busy.Length > 0 && UnityEngine.Random.value < 0.6f) {
    var u = busy[UnityEngine.Random.Range(0, busy.Length)];
    cancelled = u.currentMasterTask.description;
    u.currentMasterTask.CancelTask();
}

var prm = UnityEngine.Object.FindAnyObjectByType<PathRequestManager>();
var f = BindingFlags.NonPublic | BindingFlags.Instance;
bool processing = (bool)typeof(PathRequestManager).GetField("isProcessingPath", f).GetValue(prm);
int pending = ((System.Collections.ICollection)typeof(PathRequestManager).GetField("pathRequestQueue", f).GetValue(prm)).Count;
var listMap = (System.Collections.IDictionary)typeof(TaskQueueManager).GetField("taskListMap", f).GetValue(queue);
int queued = 0; foreach (System.Collections.DictionaryEntry kv in listMap) queued += ((System.Collections.ICollection)kv.Value).Count;
double dishAge = (DateTime.Now - queue.lastDished).TotalSeconds;

return $"units={units.Length}{spawned} busy={busy.Length} cancelled='{cancelled}' dishAge={dishAge:F1}s pathBusy={processing} pathQueue={pending} queued={queued} enemies={unitManager.GetAllPlayerUnits(Unit.FactionType.Enemy).Length}";
