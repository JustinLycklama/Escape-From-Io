// Editor eval (#14): inventory of UI text across first-party UI prefabs and the two game scenes:
// count of Text / TMP_Text components by font and font size.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

var counts = new Dictionary<string, int>();
void Count(GameObject root) {
    foreach (var t in root.GetComponentsInChildren<Text>(true)) {
        var k = $"Text | {(t.font ? t.font.name : "none")} | {t.fontSize}";
        counts[k] = counts.TryGetValue(k, out var n) ? n + 1 : 1;
    }
    foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) {
        var k = $"TMP  | {(t.font ? t.font.name : "none")} | {Mathf.RoundToInt(t.fontSize)}";
        counts[k] = counts.TryGetValue(k, out var n) ? n + 1 : 1;
    }
}

foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UI", "Assets/Resources/UI" })) {
    var path = AssetDatabase.GUIDToAssetPath(guid);
    if (path.Contains("SciFi UI Pings") || path.Contains("PieChart")) continue;
    var root = PrefabUtility.LoadPrefabContents(path);
    Count(root);
    PrefabUtility.UnloadPrefabContents(root);
}
foreach (var path in new[] { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" }) {
    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    foreach (var r in scene.GetRootGameObjects()) Count(r);
}
return string.Join("\n", counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Value,4}  {kv.Key}"));
