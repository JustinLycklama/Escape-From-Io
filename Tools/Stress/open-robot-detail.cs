// Play Mode: open the robot detail panel by clicking the Mine card (as the player would), report where it opened.
using UnityEngine;
using System.Linq;
using System.Reflection;
var cell = UnityEngine.Object.FindObjectsByType<TaskAndUnitCell>(FindObjectsSortMode.None).OrderBy(c => c.transform.GetSiblingIndex()).First();
var button = (GameButton)typeof(TaskAndUnitCell).GetField("unitListButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cell);
cell.ButtonDidClick(button);
var panel = UnityEngine.Object.FindAnyObjectByType<TaskAndUnitDetailPanel>();
if (panel == null) return "detail panel did not open";
var rt = (RectTransform)panel.transform;
return $"detail panel open: anchor={rt.anchorMin} pivot={rt.pivot} pos={rt.anchoredPosition} size={rt.rect.size}";
