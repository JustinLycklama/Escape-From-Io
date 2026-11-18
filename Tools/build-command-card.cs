// Editor eval (#17): turn the pushed selection UI into a bottom-centre command card (1600x900 reference units).
//   CurrentSelectionPanel: 640x150 at bottom centre; title row, task line (only when there's a task), one row of action buttons;
//                          back button becomes a small close button in the top-right corner.
//   ActionItemCell:        no per-button robot-type icons; centred 18-size label (it already includes the hotkey, e.g. "(E)").
//   BlueprintPanel:        bottom centre too.  TaskAndUnitDetailPanel: beside the robot cards (top-left).
// Safe to re-run.
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

const float MARGIN = 20, CARD_W = 640, CARD_H = 150, GAP = 8;
var sb = new StringBuilder();

LayoutElement LE(GameObject g) { var le = g.GetComponent<LayoutElement>(); if (le == null) le = g.AddComponent<LayoutElement>(); return le; }
void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size; }
void StretchIn(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f); rt.offsetMin = rt.offsetMax = Vector2.zero; }
void NoFitter(GameObject g) { var f = g.GetComponent<AspectRatioFitter>(); if (f != null) Object.DestroyImmediate(f, true); }

// ---- ActionItemCell: label only ----
const string CELL = "Assets/UI/TableViews + Cells/ActionItemCell.prefab";
var cell = PrefabUtility.LoadPrefabContents(CELL);
cell.transform.Find("IconArea").gameObject.SetActive(false);
var textArea = cell.transform.Find("TextArea");
var tle = LE(textArea.gameObject); tle.flexibleWidth = 1;
var label = textArea.GetComponentInChildren<Text>(true);
label.fontSize = 18; label.alignment = TextAnchor.MiddleCenter; label.resizeTextForBestFit = false;
label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
PrefabUtility.SaveAsPrefabAsset(cell, CELL);
PrefabUtility.UnloadPrefabContents(cell);
sb.AppendLine("ActionItemCell: icons hidden, centred label");

// ---- CurrentSelectionPanel: bottom-centre card ----
const string SEL = "Assets/Resources/UI/CurrentSelectionPanel.prefab";
var sel = PrefabUtility.LoadPrefabContents(SEL);
Anchor((RectTransform)sel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, MARGIN), new Vector2(CARD_W, CARD_H));

var layout = sel.transform.Find("Layout");
var v = layout.GetComponent<VerticalLayoutGroup>();
v.padding = new RectOffset(14, 14, 10, 12); v.spacing = GAP;
v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

var titleEl = layout.Find("Title Element"); var tl = LE(titleEl.gameObject); tl.preferredHeight = tl.minHeight = 26; tl.flexibleHeight = 0;
var title = titleEl.GetComponentsInChildren<Text>(true).First(t => t.name == "Title");
title.fontSize = 22; title.alignment = TextAnchor.MiddleLeft; title.resizeTextForBestFit = false;

var infoEl = layout.Find("InfoElement"); var il = LE(infoEl.gameObject); il.preferredHeight = il.minHeight = 34; il.flexibleHeight = 0;

var actionsEl = layout.Find("Action List Element"); var al = LE(actionsEl.gameObject); al.preferredHeight = al.minHeight = 56; al.flexibleHeight = 1;
var actionsOuter = actionsEl.Find("ActionsList"); StretchIn((RectTransform)actionsOuter);
var actionsRow = actionsOuter.Find("ActionsList");
var vlg = actionsRow.GetComponent<VerticalLayoutGroup>(); if (vlg != null) Object.DestroyImmediate(vlg);
var h = actionsRow.GetComponent<HorizontalLayoutGroup>(); if (h == null) h = actionsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
h.spacing = GAP; h.padding = new RectOffset(0, 0, 0, 0); h.childAlignment = TextAnchor.MiddleLeft;
h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;

foreach (Transform slot in actionsRow) {
    var sl = LE(slot.gameObject); sl.preferredWidth = 140; sl.minWidth = 120; sl.flexibleWidth = 0;
    foreach (Transform inner in slot) {
        StretchIn((RectTransform)inner); NoFitter(inner.gameObject);
        var icons = inner.Find("IconArea"); if (icons != null) icons.gameObject.SetActive(false);
    }
}

var back = layout.Find("BackButton") ?? sel.transform.Find("BackButton");
var bl = LE(back.gameObject); bl.ignoreLayout = true; NoFitter(back.gameObject);
back.SetParent(sel.transform, false);
Anchor((RectTransform)back, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(30, 30));

PrefabUtility.SaveAsPrefabAsset(sel, SEL);
PrefabUtility.UnloadPrefabContents(sel);
sb.AppendLine("CurrentSelectionPanel: bottom-centre card with action row");

// ---- BlueprintPanel: bottom centre ----
const string BP = "Assets/Resources/UI/BlueprintPanel.prefab";
var bp = PrefabUtility.LoadPrefabContents(BP);
Anchor((RectTransform)bp.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, MARGIN), new Vector2(480, 320));
var bpBack = bp.transform.Find("BackButton");
if (bpBack != null) {
    var bbl = LE(bpBack.gameObject); bbl.ignoreLayout = true; NoFitter(bpBack.gameObject);
    Anchor((RectTransform)bpBack, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(30, 30));
}
PrefabUtility.SaveAsPrefabAsset(bp, BP);
PrefabUtility.UnloadPrefabContents(bp);
sb.AppendLine("BlueprintPanel: bottom centre 480x320");

// ---- TaskAndUnitDetailPanel: beside the robot cards ----
const string DET = "Assets/Resources/UI/TaskAndUnitDetailPanel.prefab";
var det = PrefabUtility.LoadPrefabContents(DET);
var detRt = (RectTransform)det.transform;
Anchor(detRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(MARGIN + 300 + 12, -(MARGIN + 52 + 12)), detRt.sizeDelta);
PrefabUtility.SaveAsPrefabAsset(det, DET);
PrefabUtility.UnloadPrefabContents(det);
sb.AppendLine("TaskAndUnitDetailPanel: top-left beside the cards");

AssetDatabase.SaveAssets();
return sb.ToString();
