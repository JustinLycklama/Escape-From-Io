// Editor eval (#14): set CanvasScaler on the root screen-space canvases of the game scenes.
// Edit REFERENCE/MATCH below, run, then screenshot. Idempotent.
// Run: unity command script eval --file Tools/set-canvas-scaling.cs
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

var REFERENCE = new Vector2(1600, 900);
float MATCH = 0.5f;

var sb = new StringBuilder();
foreach (var path in new[] { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" }) {
    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
        if (!c.isRootCanvas || c.renderMode == RenderMode.WorldSpace) continue;
        var s = c.GetComponent<CanvasScaler>();
        if (s == null) continue;
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = REFERENCE;
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = MATCH;
        EditorUtility.SetDirty(s);
        sb.AppendLine($"{path}: {c.name} -> ref {REFERENCE} match {MATCH}");
    }
    EditorSceneManager.MarkSceneDirty(scene);
    EditorSceneManager.SaveScene(scene);
}
return sb.ToString();
