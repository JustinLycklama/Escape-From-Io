// Editor eval: describe the HUD's settings/play/pause buttons, the time panel and the threat cell (children, sprites, sizes).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
var sb = new StringBuilder();
void Dump(Transform t, int d, int max) {
    if (d > max) return;
    var img = t.GetComponent<Image>(); var txt = t.GetComponent<Text>(); var rt = (RectTransform)t;
    sb.AppendLine($"{new string(' ', d * 2)}{(t.gameObject.activeSelf ? "" : "[off] ")}{t.name} size={rt.rect.width:0}x{rt.rect.height:0}" +
        (img ? $" img={(img.sprite ? img.sprite.name : "none")}/{img.type}/col={ColorUtility.ToHtmlStringRGBA(img.color)}" : "") +
        (txt ? $" text='{txt.text}' font={txt.font?.name} size={txt.fontSize}" : "") +
        $" [{string.Join(",", t.GetComponents<Component>().Where(c => c && !(c is Transform) && !(c is CanvasRenderer) && !(c is Image) && !(c is Text)).Select(c => c.GetType().Name))}]");
    foreach (Transform c in t) Dump(c, d + 1, max);
}
var settings = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
sb.AppendLine("== SettingsPanel"); Dump(settings.transform, 0, 3);
sb.AppendLine("== TimePanel"); Dump(Object.FindAnyObjectByType<TimePanel>(FindObjectsInactive.Include).transform, 0, 3);
sb.AppendLine("== AttackersCell"); Dump(Object.FindAnyObjectByType<AttackersCell>(FindObjectsInactive.Include).transform, 0, 3);
sb.AppendLine("== ResourcesPanel"); Dump(Object.FindAnyObjectByType<ResourcesPanel>(FindObjectsInactive.Include).transform, 0, 2);
return sb.ToString();
