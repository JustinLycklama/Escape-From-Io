// Edit or Play Mode: dump the RobotCards container and each card's rect, plus animator info (#16 layout debugging).
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

var sb = new StringBuilder();
var cards = GameObject.Find("RobotCards");
if (cards == null) return "RobotCards not found";
void Line(RectTransform t, int d) {
    var anim = t.GetComponent<Animator>();
    var le = t.GetComponent<LayoutElement>();
    sb.AppendLine($"{new string(' ', d * 2)}{(t.gameObject.activeInHierarchy ? "" : "[inactive] ")}{t.name} a=({t.anchorMin.x:0.##},{t.anchorMin.y:0.##})-({t.anchorMax.x:0.##},{t.anchorMax.y:0.##}) pivot={t.pivot} pos={t.anchoredPosition} rect={t.rect.width:0}x{t.rect.height:0} scale={t.localScale.x:0.##}" +
        (anim ? $" animator(enabled={anim.enabled}, ctrl={(anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "none")})" : "") +
        (le ? $" LE(ignore={le.ignoreLayout} pw={le.preferredWidth} ph={le.preferredHeight})" : "") +
        (t.GetComponent<LayoutGroup>() is LayoutGroup g ? $" {g.GetType().Name}(enabled={g.enabled})" : ""));
}
var root = (RectTransform)cards.transform;
Line(root, 0);
foreach (RectTransform c in root) {
    Line(c, 1);
    foreach (RectTransform cc in c) {
        Line(cc, 2);
        foreach (RectTransform ccc in cc) Line(ccc, 3);
    }
}
return sb.ToString();
