// Inspect task-assignment state in Play Mode: queued tasks per list, waiting units and their refusals, and a direct path test.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

var sb = new StringBuilder();
var f = BindingFlags.NonPublic | BindingFlags.Instance;
var queue = UnityEngine.Object.FindAnyObjectByType<TaskQueueManager>();

var listMap = (IDictionary)typeof(TaskQueueManager).GetField("taskListMap", f).GetValue(queue);
foreach (DictionaryEntry kv in listMap) {
    var list = (List<MasterGameTask>)kv.Value;
    sb.AppendLine($"list {kv.Key}: {list.Count} tasks, locked={queue.GetTaskListLockStatus((MasterGameTask.ActionType)kv.Key)}");
    foreach (var t in list.Take(4)) {
        var g = t.childGameTasks.FirstOrDefault();
        sb.AppendLine($"   #{t.taskNumber} '{t.description}' blockers={t.blockerTasks.Count} startOK={t.SatisfiesStartRequirements()} last={t.alwaysPerformLast} first={g?.action}/{g?.pathRequestTargetType}");
    }
}

var requesting = (IList)typeof(TaskQueueManager).GetField("unitsRequestingTasks", f).GetValue(queue);
sb.AppendLine($"units requesting: {requesting.Count}");
foreach (var r in requesting) {
    var unit = (Unit)r.GetType().GetField("unit").GetValue(r);
    var refused = (HashSet<int>)r.GetType().GetField("refuseTaskList").GetValue(r);
    sb.AppendLine($"   {(unit == null ? "<destroyed>" : unit.title)} primary={unit?.primaryActionType} refused=[{string.Join(",", refused)}]");
}

var units = UnityEngine.Object.FindAnyObjectByType<UnitManager>().GetAllPlayerUnits();
foreach (var u in units) {
    sb.AppendLine($"unit {u.title} task={(u.currentMasterTask?.description ?? "-")} dur={u.remainingDuration} pos={u.transform.position}");
}

// Direct A* test from the first unit to a few points around it
var pf = UnityEngine.Object.FindAnyObjectByType<PathFinding>();
var grid = UnityEngine.Object.FindAnyObjectByType<PathfindingGrid>();
if (units.Length > 0) {
    var start = units[0].transform.position;
    var sNode = grid.nodeFromWorldPoint(new WorldPosition(start));
    sb.AppendLine($"start node walkable={sNode.walkable} penalty={sNode.movementPenalty}");
    foreach (var off in new[] { new Vector3(3, 0, 0), new Vector3(0, 0, 3), new Vector3(-6, 0, 0), new Vector3(0, 0, -6) }) {
        var target = start + off;
        bool done = false, ok = false; int dist = 0;
        pf.FindSimplifiedPath(start, target, 1, (pts, success, d) => { done = true; ok = success; dist = d; });
        var tNode = grid.nodeFromWorldPoint(new WorldPosition(target));
        sb.AppendLine($"path to {off}: done={done} success={ok} dist={dist} targetWalkable={tNode.walkable}");
    }
}
return sb.ToString();
