// Editor eval: dump prefab hierarchies (PREFABS below) with active state, anchors, rect size, layout components, sprites/text.
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

var PREFABS = new[] { "Assets/Resources/UI/CurrentSelectionPanel.prefab", "Assets/Resources/UI/BlueprintPanel.prefab", "Assets/UI/TableViews + Cells/ActionItemCell.prefab" };
const int DEPTH = 5;
var sb = new StringBuilder();
void Dump(RectTransform t, int d) {
    if (d > DEPTH) return;
    var g = t.gameObject;
    var comps = string.Join(",", g.GetComponents<Component>().Where(c => c && !(c is Transform) && !(c is CanvasRenderer) && !(c is Image) && !(c is Text)).Select(c => c.GetType().Name));
    var img = g.GetComponent<Image>(); var txt = g.GetComponent<Text>();
    sb.AppendLine($"{new string(' ', d * 2)}{(g.activeSelf ? "" : "[off] ")}{g.name} a=({t.anchorMin.x:0.##},{t.anchorMin.y:0.##})-({t.anchorMax.x:0.##},{t.anchorMax.y:0.##}) pos={t.anchoredPosition.x:0},{t.anchoredPosition.y:0} size={t.sizeDelta.x:0},{t.sizeDelta.y:0}" +
        (img && img.sprite ? $" img={img.sprite.name}" : "") + (txt ? $" text='{txt.text}'({txt.fontSize})" : "") + (comps.Length > 0 ? $" [{comps}]" : ""));
    foreach (RectTransform c in t) Dump(c, d + 1);
}
foreach (var p in PREFABS) {
    sb.AppendLine($"== {p}");
    var root = PrefabUtility.LoadPrefabContents(p);
    Dump((RectTransform)root.transform, 0);
    PrefabUtility.UnloadPrefabContents(root);
}
return sb.ToString();
