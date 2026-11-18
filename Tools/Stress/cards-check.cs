// Play Mode check (#16): each robot card's count/queued text, lock state vs TaskQueueManager, caption; then toggle Mine's lock
// through the card (as a click would) and confirm the queue lock and caption flip, and flip it back.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;
using System.Text;

var sb = new StringBuilder();
var queue = UnityEngine.Object.FindAnyObjectByType<TaskQueueManager>();
var f = BindingFlags.NonPublic | BindingFlags.Instance;
T Get<T>(TaskAndUnitCell c, string name) => (T)typeof(TaskAndUnitCell).GetField(name, f).GetValue(c);

string Describe(TaskAndUnitCell c) {
    var type = Get<MasterGameTask.ActionType>(c, "actionType");
    var rt = (RectTransform)c.transform;
    return $"{type}: units='{Get<Text>(c, "unitsCountText").text}' queued='{Get<Text>(c, "taskCountText").text}' " +
           $"locked(queue)={queue.GetTaskListLockStatus(type)} toggle={Get<Toggle>(c, "taskListLockToggle").state} caption='{Get<Text>(c, "lockStateLabel")?.text}' rect={rt.rect.width:0}x{rt.rect.height:0}";
}

var cells = UnityEngine.Object.FindObjectsByType<TaskAndUnitCell>(FindObjectsSortMode.None).OrderBy(c => c.transform.GetSiblingIndex()).ToArray();
foreach (var c in cells) sb.AppendLine(Describe(c));

var mine = cells.First(c => Get<MasterGameTask.ActionType>(c, "actionType") == MasterGameTask.ActionType.Mine);
var lockButton = Get<GameButton>(mine, "taskListLockToggle");
mine.ButtonDidClick(lockButton);
sb.AppendLine("after toggle: " + Describe(mine));
mine.ButtonDidClick(lockButton);
sb.AppendLine("after toggle back: " + Describe(mine));
return sb.ToString();
