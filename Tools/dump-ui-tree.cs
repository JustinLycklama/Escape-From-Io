// Editor eval: dump a scene's screen-space canvas tree (to DEPTH) with active state, anchors, size, position and key components.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
using System.Text;

const int DEPTH = 6;
const string SCENE = "Assets/Scenes/TitleScene.unity";   // or GameScene.unity
var sb = new StringBuilder();
EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
    .First(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay);

string Comps(GameObject g) => string.Join(",", g.GetComponents<Component>()
    .Where(c => c != null && !(c is Transform) && !(c is CanvasRenderer))
    .Select(c => c.GetType().Name)
    .Where(n => n != "Image" && n != "LayoutElement"));

void Dump(RectTransform t, int d) {
    if (d > DEPTH) return;
    var g = t.gameObject;
    sb.AppendLine($"{new string(' ', d * 2)}{(g.activeSelf ? "" : "[off] ")}{g.name}  a=({t.anchorMin.x:0.##},{t.anchorMin.y:0.##})-({t.anchorMax.x:0.##},{t.anchorMax.y:0.##}) size={t.sizeDelta.x:0},{t.sizeDelta.y:0} pos={t.anchoredPosition.x:0},{t.anchoredPosition.y:0}  [{Comps(g)}]");
    foreach (RectTransform c in t) Dump(c, d + 1);
}
Dump((RectTransform)canvas.transform, 0);
return sb.ToString();
