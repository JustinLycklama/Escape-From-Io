// Editor eval (#16): turn the TaskAndUnitCell prefab into a compact card and stack the three cards on the left edge.
// Card (1600x900 reference units, 300x72): [type icon] [robots: title, count ring, battery bars] [tasks: queued count, lock].
// The flow arrows and per-type icons are hidden (the script still updates them). The lock toggle shows a padlock
// when locked to its own type and arrows when shared, with a caption.
// The prefab step converts the original prefab once (restore it from git to rebuild); the whole script is safe to re-run.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

const string PREFAB = "Assets/UI/Scale To Parent Cells/TaskAndUnitCell.prefab";
const float CARD_W = 300, CARD_H = 72, GAP = 8, MARGIN = 20, TOP_BAR = 52, BELOW_BAR = 12;
var sb = new StringBuilder();

var lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Assets/Icons/lock.png");
var sharedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Assets/Icons/arrows.png");
var agency = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Assets/Fonts/AgencyFB.ttf");

LayoutElement LE(GameObject g) { var le = g.GetComponent<LayoutElement>(); if (le == null) le = g.AddComponent<LayoutElement>(); return le; }
void Width(Transform t, float preferred, float flexible) { var le = LE(t.gameObject); le.preferredWidth = preferred; le.minWidth = preferred * 0.8f; le.flexibleWidth = flexible; }
Transform Find(Transform root, string path) { var t = root.Find(path); if (t == null) throw new System.Exception("missing " + path); return t; }

// ---- Prefab: flat card layout ----
// The original cell is nested auto-layouts sized for a 320x120 row; squeezed to 72 tall they overlap. Instead, lift the
// existing, already-wired elements out onto a flat "Card" and place them with explicit rects (top-left anchored).
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var cellScript = root.GetComponent<TaskAndUnitCell>();
var so = new SerializedObject(cellScript);
Object Ref(string field) => so.FindProperty(field).objectReferenceValue;
Transform RefT(string field) => ((Component)Ref(field)).transform;

var oldCard = root.transform.Find("Card");
bool prefabDone = oldCard != null;
if (prefabDone) {
    PrefabUtility.UnloadPrefabContents(root);
    sb.AppendLine("prefab already converted (restore it from git to rebuild the card)");
} else {

var card = new GameObject("Card", typeof(RectTransform)).transform as RectTransform;
card.gameObject.layer = LayerMask.NameToLayer("UI");
card.SetParent(root.transform, false);
card.anchorMin = Vector2.zero; card.anchorMax = Vector2.one; card.offsetMin = card.offsetMax = Vector2.zero;

// Place a rect at (x, y) from the card's top-left, size (w, h). Stops any layout/aspect component from fighting it.
void Put(Transform t, float x, float y, float w, float h) {
    t.SetParent(card, false);
    var rt = (RectTransform)t;
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
    rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
    var le = t.GetComponent<LayoutElement>(); if (le != null) le.ignoreLayout = true;
    // Remove (not just disable) aspect fitters: a disabled fitter still leaves the rect height driven to 0 at runtime
    var arf = t.GetComponent<AspectRatioFitter>(); if (arf != null) Object.DestroyImmediate(arf, true);
}
void Stretch(Transform t) {
    t.SetParent(card, false);
    var rt = (RectTransform)t;
    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
}

// Background + click target for details: the robots panel button image, across the whole card
var cardButton = RefT("unitListButton");          // UnitsDetailsLayout (Image + GameButton)
var unitsTitle = (Text)Ref("unitsTitle");
var pieBorder = ((Component)Ref("pieChart")).transform.parent;   // Border: ring frame + PieChart + count Text
var bars = so.FindProperty("percentBars");
var taskPanel = RefT("taskListLockButton");        // TasksDetailsLayout (Image tinted by lock state + GameButton)
var countCircle = RefT("taskCountText").parent;    // count ring background holding taskCountText
var toggle = (Toggle)Ref("taskListLockToggle");
var unitIcon = RefT("unitIconImage");

Stretch(cardButton);
foreach (Transform child in cardButton) child.gameObject.SetActive(false);   // old Inset contents are moved out below

Put(unitIcon, 8, 10, 52, 52);
Put(unitsTitle.transform, 68, 6, 124, 22);
unitsTitle.alignment = TextAnchor.MiddleLeft; unitsTitle.resizeTextForBestFit = false; unitsTitle.fontSize = 18;
Put(pieBorder, 68, 30, 36, 36);
for (int i = 0; i < bars.arraySize; i++) {
    var bar = ((Component)bars.GetArrayElementAtIndex(i).objectReferenceValue).transform;
    Put(bar, 110 + i * 15, 30, 12, 36);
}

Put(taskPanel, 198, 4, 98, 64);
foreach (Transform child in taskPanel) child.gameObject.SetActive(false);
Put(countCircle, 210, 8, 30, 30);
Put(toggle.transform, 254, 9, 28, 28);
var toggleImage = toggle.GetComponent<Image>();
toggleImage.sprite = lockSprite; toggleImage.preserveAspect = true;
toggle.mouseOver = lockSprite; toggle.click = lockSprite;
toggle.off = sharedSprite; toggle.offMouseOver = sharedSprite; toggle.offClick = sharedSprite;

var captionGo = new GameObject("LockCaption", typeof(RectTransform));
captionGo.layer = LayerMask.NameToLayer("UI");
var caption = captionGo.AddComponent<Text>();
caption.font = agency; caption.fontSize = 14; caption.alignment = TextAnchor.MiddleCenter;
caption.color = new Color(0.86f, 0.94f, 1f, 1f); caption.raycastTarget = false;
caption.horizontalOverflow = HorizontalWrapMode.Overflow; caption.verticalOverflow = VerticalWrapMode.Overflow; caption.text = "Own type only";
Put(captionGo.transform, 200, 41, 94, 16);

// Everything still referenced but no longer shown: the old row container (flow arrows, type icons, titles)
Find(root.transform, "DetailsLayoutElement").gameObject.SetActive(false);

so.FindProperty("lockStateLabel").objectReferenceValue = caption;
so.ApplyModifiedPropertiesWithoutUndo();

PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
sb.AppendLine("prefab: flat card (icon, title, count ring, battery bars | queued count, lock, caption)");

}

// ---- Scene: stack cards on the left edge ----
var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay);
var guiOverlay = canvas.transform.Find("GUI Overlay Panel");
var cells = Object.FindObjectsByType<TaskAndUnitCell>(FindObjectsInactive.Include, FindObjectsSortMode.None);
// Scene instances carry stale per-instance RectTransform overrides from the old row layout (e.g. heights of 0)
// that would beat the prefab's new card rects. Revert those, but keep other overrides such as each card's actionType.
int reverted = 0;
foreach (var cell in cells) {
    foreach (var rt in cell.GetComponentsInChildren<RectTransform>(true)) {
        if (rt == cell.transform) continue;   // the card's own rect is driven by the stack layout
        if (PrefabUtility.IsPartOfPrefabInstance(rt) && PrefabUtility.HasPrefabInstanceAnyOverrides(PrefabUtility.GetOutermostPrefabInstanceRoot(rt), false)) {
            PrefabUtility.RevertObjectOverride(rt, InteractionMode.AutomatedAction);
            reverted++;
        }
    }
}
sb.AppendLine($"reverted RectTransform overrides on {reverted} card elements");

var area = cells[0].transform.parent.parent;   // TaskAndUnitCellArea (cells sit in its Layout child)

var cards = guiOverlay.Find("RobotCards") as RectTransform;
if (cards == null) {
    var go = new GameObject("RobotCards", typeof(RectTransform));
    go.layer = LayerMask.NameToLayer("UI");
    cards = (RectTransform)go.transform;
    cards.SetParent(guiOverlay, false);
}
cards.anchorMin = cards.anchorMax = new Vector2(0, 1); cards.pivot = new Vector2(0, 1);
cards.anchoredPosition = new Vector2(MARGIN, -(MARGIN + TOP_BAR + BELOW_BAR));
cards.sizeDelta = new Vector2(CARD_W, CARD_H * cells.Length + GAP * (cells.Length - 1));

area.SetParent(cards, false);
var areaRt = (RectTransform)area;
areaRt.anchorMin = Vector2.zero; areaRt.anchorMax = Vector2.one; areaRt.pivot = new Vector2(0.5f, 0.5f);
areaRt.offsetMin = areaRt.offsetMax = Vector2.zero;
EditorUtility.SetDirty(areaRt);
Canvas.ForceUpdateCanvases();
if (areaRt.rect.width < 1 || areaRt.rect.height < 1) throw new System.Exception($"card area did not stretch: {areaRt.rect.size}");
var areaLe = area.GetComponent<LayoutElement>(); if (areaLe != null) areaLe.ignoreLayout = true;

var stack = area.Find("Layout").GetComponent<VerticalLayoutGroup>();
stack.spacing = GAP; stack.padding = new RectOffset(0, 0, 0, 0);
stack.childControlWidth = stack.childControlHeight = true;
stack.childForceExpandWidth = stack.childForceExpandHeight = true;

EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
sb.AppendLine($"scene: {cells.Length} cards stacked at left ({cards.sizeDelta.x}x{cards.sizeDelta.y})");
return sb.ToString();
