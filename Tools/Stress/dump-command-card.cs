// Play Mode: dump the live CurrentSelectionPanel (command card): active state, rects, action labels, task line state.
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

var panel = UnityEngine.Object.FindAnyObjectByType<CurrentSelectionPanel>();
if (panel == null) return "no CurrentSelectionPanel (nothing selected?)";
var sb = new StringBuilder();
void D(RectTransform t, int d, int max) {
    if (d > max) return;
    var txt = t.GetComponent<Text>();
    sb.AppendLine($"{new string(' ', d * 2)}{(t.gameObject.activeSelf ? "" : "[off] ")}{t.name} rect={t.rect.width:0}x{t.rect.height:0} pos={t.anchoredPosition}" + (txt ? $" text='{txt.text}'" : ""));
    foreach (RectTransform c in t) D(c, d + 1, max);
}
D((RectTransform)panel.transform, 0, 5);
return sb.ToString();
