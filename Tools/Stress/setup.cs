// Stress setup: run once in Play Mode after GameScene finishes initializing.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
// Queues player-style Mine/Clean actions around spawn, places a few Light Towers, unlocks all task lists.
using UnityEngine;
using System.Linq;

// Narrator.gameInitialized is never set, so wait for the intro to unpause the game with units registered.
var player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
var unitManager = UnityEngine.Object.FindAnyObjectByType<UnitManager>();
if (player == null || unitManager == null || player.gamePaused || unitManager.GetAllPlayerUnits().Length == 0) { return "not initialized yet"; }

var constants = UnityEngine.Object.FindAnyObjectByType<Constants>();
var mapGen = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
var terrain = UnityEngine.Object.FindAnyObjectByType<TerrainManager>();
var buildings = UnityEngine.Object.FindAnyObjectByType<BuildingManager>();
var queue = UnityEngine.Object.FindAnyObjectByType<TaskQueueManager>();
var spawn = mapGen.spawnCoordinate;

int actions = 0, towers = 0;
for (int dx = -6; dx <= 6; dx++) {
    for (int dy = -6; dy <= 6; dy++) {
        int x = spawn.x + dx, y = spawn.y + dy;
        if (x < 0 || y < 0 || x >= constants.layoutMapWidth || y >= constants.layoutMapHeight) continue;
        var c = new LayoutCoordinate(x, y, spawn.mapContainer);
        if (buildings.buildingAtLocation(c) != null) continue;

        var act = terrain.ActionsAvailableForTerrain(c)?.FirstOrDefault(a =>
            a.tutorialIdentifier == UserAction.UserActionTutorialIdentifier.Mine ||
            a.tutorialIdentifier == UserAction.UserActionTutorialIdentifier.Clean);
        if (act != null) { act.performAction(c); actions++; continue; }

        if (towers < 3 && Mathf.Abs(dx) + Mathf.Abs(dy) >= 3 && mapGen.GetTerrainAt(c).buildable) {
            Building.Blueprint.Tower.ConstructAt(c); towers++;
        }
    }
}

foreach (MasterGameTask.ActionType t in new[] { MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Move }) {
    queue.SetTaskListLocked(t, false);
}
// Disable enemies so unit deaths come only from battery: EnemyManager spawns solely via its TimeManager subscription.
var enemyManager = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
UnityEngine.Object.FindAnyObjectByType<TimeManager>().EndTimeUpdateNotifications(enemyManager);
int golems = 0;
foreach (var e in unitManager.GetAllPlayerUnits(Unit.FactionType.Enemy)) { e.Shutdown(); golems++; }

return $"queued {actions} terrain actions, {towers} towers; lists unlocked; enemies disabled ({golems} removed)";
