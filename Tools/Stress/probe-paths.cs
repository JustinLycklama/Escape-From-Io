// For each queued task, request a path from the first idle unit exactly as Unit.RequestPath does, and report the result.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

var sb = new StringBuilder();
var f = BindingFlags.NonPublic | BindingFlags.Instance;
var queue = UnityEngine.Object.FindAnyObjectByType<TaskQueueManager>();
var unit = UnityEngine.Object.FindAnyObjectByType<UnitManager>().GetAllPlayerUnits().FirstOrDefault();
if (unit == null) return "no units";

var grid = UnityEngine.Object.FindAnyObjectByType<PathfindingGrid>();
var listMap = (IDictionary)typeof(TaskQueueManager).GetField("taskListMap", f).GetValue(queue);
foreach (DictionaryEntry kv in listMap) {
    foreach (var t in ((List<MasterGameTask>)kv.Value).Take(5)) {
        var g = t.childGameTasks[0];
        bool fired = false, ok = false; int dist = 0; int pts = -1;
        var req = PathRequestManager.RequestPathForTask(unit.transform.position, unit.movementPenaltyMultiplier, g, (lp, item, success, d) => { fired = true; ok = success; dist = d; pts = lp?.Length ?? -1; });
        var tNode = grid.nodeFromWorldPoint(g.target);
        sb.AppendLine($"#{t.taskNumber} {g.action}/{g.pathRequestTargetType} target={g.target.vector3} targetNodeWalkable={tNode.walkable} -> request={(req != null)} fired={fired} success={ok} dist={dist} points={pts}");
    }
}
var uNode = grid.nodeFromWorldPoint(new WorldPosition(unit.transform.position));
sb.AppendLine($"unit {unit.title} at {unit.transform.position} nodeWalkable={uNode.walkable} penaltyMult={unit.movementPenaltyMultiplier}");
return sb.ToString();
