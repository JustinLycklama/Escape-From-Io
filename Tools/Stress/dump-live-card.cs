// Play Mode: dump the first live robot card's Card children (active, rect, image/text state, CanvasRenderer alpha/cull).
using UnityEngine; using UnityEngine.UI; using System.Linq; using System.Text;
var cell = UnityEngine.Object.FindObjectsByType<TaskAndUnitCell>(FindObjectsSortMode.None).OrderBy(c => c.transform.GetSiblingIndex()).First();
var sb = new StringBuilder();
foreach (RectTransform t in cell.transform.Find("Card")) {
    var img = t.GetComponent<Image>(); var txt = t.GetComponent<Text>(); var cr = t.GetComponent<CanvasRenderer>();
    sb.AppendLine($"{(t.gameObject.activeInHierarchy ? "" : "[inactive] ")}{t.name} pos={t.anchoredPosition} rect={t.rect.width:0}x{t.rect.height:0} world={t.position}" +
      (img ? $" img(en={img.enabled},sprite={(img.sprite ? img.sprite.name : "none")},col={img.color})" : "") +
      (txt ? $" text('{txt.text}',col={txt.color})" : "") + (cr ? $" cr(alpha={cr.GetAlpha():0.##},cull={cr.cull})" : ""));
}
return sb.ToString();
