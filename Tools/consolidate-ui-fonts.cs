// Editor eval (#14): consolidate UI fonts to Agency FB (text) + DS-DIGI (numeric readouts, untouched).
// Helvetica / Arial (LegacyRuntime) / Bahnschrift -> AgencyFB; Impact -> AgencyFB-Bold. Sizes are left as they are.
// Covers first-party UI prefabs (Assets/UI, Assets/Resources/UI) and the two game scenes. Idempotent.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

var regular = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Assets/Fonts/AgencyFB.ttf");
var bold = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Assets/Fonts/AgencyFB-Bold.otf");
var map = new Dictionary<string, Font> {
    { "Helvetica", regular }, { "LegacyRuntime", regular }, { "Arial", regular }, { "bahnschrift", regular }, { "impact", bold }
};

int Swap(GameObject root) {
    int n = 0;
    foreach (var t in root.GetComponentsInChildren<Text>(true)) {
        if (t.font != null && map.TryGetValue(t.font.name, out var target)) {
            Undo.RecordObject(t, "font");
            t.font = target;
            EditorUtility.SetDirty(t);
            n++;
        }
    }
    return n;
}

var report = new List<string>();
foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UI", "Assets/Resources/UI" })) {
    var path = AssetDatabase.GUIDToAssetPath(guid);
    if (path.Contains("SciFi UI Pings") || path.Contains("PieChart")) continue;
    var root = PrefabUtility.LoadPrefabContents(path);
    int n = Swap(root);
    if (n > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); report.Add($"{n,3}  {path}"); }
    PrefabUtility.UnloadPrefabContents(root);
}
foreach (var path in new[] { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" }) {
    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    int n = scene.GetRootGameObjects().Sum(Swap);
    if (n > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); report.Add($"{n,3}  {path}"); }
}
AssetDatabase.SaveAssets();
return string.Join("\n", report);
