// Play-mode eval (#21): report each robot's overhead marker alpha, then select the first player robot.
// Run twice: the second run should show that robot's marker at alpha 1 while the others stay hidden unless hovered or needing attention.
using UnityEngine;
using System.Linq;
using System.Text;

if (!Object.FindAnyObjectByType<Narrator>().gameInitialized) return "game not initialized yet";
var sb = new StringBuilder();
var mouse = UnityEngine.InputSystem.Mouse.current?.position.ReadValue();
sb.AppendLine($"mouse={mouse}");
foreach (var t in Object.FindObjectsByType<UnitStatusTooltip>(FindObjectsSortMode.None)) {
    var u = t.owner;
    string state = u == null ? "?" : $"{u.name} {u.GetUnitState()} battery={u.remainingDuration}";
    sb.AppendLine($"  alpha={t.canvasGroup.alpha:0.00}  {state}");
}
var sel = Object.FindAnyObjectByType<SelectionManager>();
var first = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).FirstOrDefault(u => u.factionType == Unit.FactionType.Player && u.initialized);
if (first != null) { sel.SelectSelectable(first); sb.AppendLine($"selected {first.name}"); }
return sb.ToString();
