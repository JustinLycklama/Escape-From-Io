// Regression check for #2: a path callback that throws must not freeze PathRequestManager.
// Requests a path whose callback throws, then a normal one, and reports whether the second completed.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Linq;

var unit = UnityEngine.Object.FindAnyObjectByType<UnitManager>().GetAllPlayerUnits().FirstOrDefault();
if (unit == null) return "no units";

var start = unit.transform.position;
var target = new WorldPosition(start + new Vector3(10, 0, 0));
var task = new GameTask("throw test", target, GameTask.ActionType.Build, null);

PathRequestManager.RequestPathForTask(start, 1, task, (lp, item, ok, d) => { throw new System.Exception("intentional test exception (#2 regression check)"); });

bool secondFired = false;
PathRequestManager.RequestPathForTask(start, 1, task, (lp, item, ok, d) => { secondFired = true; });

return $"after throwing callback: secondFired={secondFired} {PathRequestManager.DiagnosticSummary()}";
