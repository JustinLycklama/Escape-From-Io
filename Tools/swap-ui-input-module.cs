// Editor eval (#8): replace StandaloneInputModule with InputSystemUIInputModule on every EventSystem in the game scenes.
// Idempotent. Run: unity command script eval --file Tools/swap-ui-input-module.cs
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Text;

var sb = new StringBuilder();
foreach (var path in new[] { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" }) {
    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    int swapped = 0;
    foreach (var es in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) { UnityEngine.Object.DestroyImmediate(legacy); }
        if (es.GetComponent<InputSystemUIInputModule>() == null) {
            es.gameObject.AddComponent<InputSystemUIInputModule>();
            swapped++;
        }
    }
    EditorSceneManager.MarkSceneDirty(scene);
    EditorSceneManager.SaveScene(scene);
    sb.AppendLine($"{path}: {swapped} EventSystem(s) switched");
}

// Prefabs that carry their own EventSystem
foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UI", "Assets/Resources" })) {
    var p = AssetDatabase.GUIDToAssetPath(guid);
    var root = PrefabUtility.LoadPrefabContents(p);
    bool changed = false;
    foreach (var legacy in root.GetComponentsInChildren<StandaloneInputModule>(true)) {
        var go = legacy.gameObject;
        UnityEngine.Object.DestroyImmediate(legacy);
        if (go.GetComponent<InputSystemUIInputModule>() == null) go.AddComponent<InputSystemUIInputModule>();
        changed = true;
    }
    if (changed) { PrefabUtility.SaveAsPrefabAsset(root, p); sb.AppendLine($"{p}: switched"); }
    PrefabUtility.UnloadPrefabContents(root);
}
return sb.ToString();
