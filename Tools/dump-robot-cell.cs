// Editor eval (#16): dump the first TaskAndUnitCell in GameScene: hierarchy, rect anchors/sizes, layout components, and which
// children are referenced by the TaskAndUnitCell script's serialized fields.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using System.Collections.Generic;

EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
var cell = Object.FindObjectsByType<TaskAndUnitCell>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
var sb = new StringBuilder();

// map referenced objects -> field names
var refs = new Dictionary<Object, string>();
var so = new SerializedObject(cell);
var it = so.GetIterator();
while (it.NextVisible(true)) {
    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null) {
        var o = it.objectReferenceValue is Component c ? (Object)c.gameObject : it.objectReferenceValue;
        refs[o] = (refs.TryGetValue(o, out var e) ? e + "," : "") + it.propertyPath;
    }
}

void Dump(RectTransform t, int d) {
    var g = t.gameObject;
    var comps = string.Join(",", g.GetComponents<Component>().Where(c => c && !(c is Transform) && !(c is CanvasRenderer)).Select(c => c.GetType().Name));
    var img = g.GetComponent<Image>();
    string r = refs.TryGetValue(g, out var f) ? $"  <= {f}" : "";
    sb.AppendLine($"{new string(' ', d * 2)}{(g.activeSelf ? "" : "[off] ")}{g.name} a=({t.anchorMin.x:0.##},{t.anchorMin.y:0.##})-({t.anchorMax.x:0.##},{t.anchorMax.y:0.##}) rect={t.rect.width:0}x{t.rect.height:0}" +
        (img && img.sprite ? $" sprite={img.sprite.name}" : "") + $" [{comps}]{r}");
    foreach (RectTransform c in t) Dump(c, d + 1);
}
Dump((RectTransform)cell.transform, 0);
sb.AppendLine($"cell parent: {cell.transform.parent.name} > {cell.transform.parent.parent.name}; prefab instance: {PrefabUtility.IsPartOfPrefabInstance(cell.gameObject)}");
return sb.ToString();
