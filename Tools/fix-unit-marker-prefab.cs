// Editor eval (#21): make the robot overhead marker (Resources/UI/UnitStatusPanel) fade as one piece.
// Originally only the task cell ("TopArea") faded on hover; the type icon and battery/health bars sat under CanvasGroups with
// ignoreParentGroups, so they were always visible. This puts a CanvasGroup on the root, points UnitStatusTooltip at it,
// and lets the child groups inherit it. Safe to re-run.
using UnityEditor;
using UnityEngine;
using System.Text;

const string PATH = "Assets/Resources/UI/UnitStatusPanel.prefab";
var sb = new StringBuilder();
var root = PrefabUtility.LoadPrefabContents(PATH);
try {
    foreach (var g in root.GetComponentsInChildren<CanvasGroup>(true)) {
        sb.AppendLine($"before: {g.name} alpha={g.alpha} ignoreParent={g.ignoreParentGroups}");
        g.ignoreParentGroups = false;
    }
    var rootGroup = root.GetComponent<CanvasGroup>();
    if (rootGroup == null) rootGroup = root.AddComponent<CanvasGroup>();
    rootGroup.alpha = 0; rootGroup.interactable = false; rootGroup.blocksRaycasts = false;
    var tooltip = root.GetComponent<UnitStatusTooltip>();
    var top = tooltip.canvasGroup;
    if (top != null && top != rootGroup) top.alpha = 1;
    var so = new SerializedObject(tooltip);
    so.FindProperty("canvas").objectReferenceValue = rootGroup;
    so.ApplyModifiedPropertiesWithoutUndo();
    PrefabUtility.SaveAsPrefabAsset(root, PATH);
    sb.AppendLine($"root CanvasGroup drives the marker; previous group '{(top ? top.name : "none")}' now inherits it");
} finally {
    PrefabUtility.UnloadPrefabContents(root);
}
return sb.ToString();
