// Editor eval: report every Canvas/CanvasScaler in the two game scenes (render mode, scale mode, reference resolution, match).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

var sb = new StringBuilder();
foreach (var path in new[] { "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" }) {
    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    sb.AppendLine($"== {path}");
    foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
        if (!c.isRootCanvas) continue;
        var s = c.GetComponent<CanvasScaler>();
        string scaler = s == null ? "no scaler" :
            $"{s.uiScaleMode} ref={s.referenceResolution} match={s.matchWidthOrHeight} scaleFactor={s.scaleFactor} screenMatch={s.screenMatchMode}";
        sb.AppendLine($"  {c.name}: {c.renderMode} sort={c.sortingOrder} pixelPerfect={c.pixelPerfect} | {scaler}");
    }
}
return sb.ToString();
