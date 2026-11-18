// Editor eval (#20): title screen layout. Large left-aligned logo above a narrow button column, both on the same left axis.
// Buttons ~360x64 at 1080p (300x54 reference units), labels at the 25 size step. Safe to re-run.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;

const float LEFT = 120, COLUMN_TOP = 60, BTN_W = 300, BTN_H = 54, GAP = 12, EXIT_GAP = 16, TITLE_GAP = 40, TITLE_SIZE = 64;
var sb = new StringBuilder();
var scene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity", OpenSceneMode.Single);
var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.isRootCanvas);
var root = canvas.transform;
var agency = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Assets/Fonts/AgencyFB.ttf");
var agencyBold = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Assets/Fonts/AgencyFB-Bold.otf");

// Button column
var panel = (RectTransform)root.Find("Action Panel");
panel.anchorMin = panel.anchorMax = new Vector2(0, 0.5f); panel.pivot = new Vector2(0, 1);
panel.anchoredPosition = new Vector2(LEFT, COLUMN_TOP);
var vlg = panel.GetComponent<VerticalLayoutGroup>();
vlg.spacing = GAP; vlg.padding = new RectOffset(); vlg.childAlignment = TextAnchor.UpperLeft;
vlg.childControlWidth = vlg.childControlHeight = true; vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;
float total = 0;
foreach (RectTransform child in panel) {
    if (!child.gameObject.activeSelf) continue;
    bool spacer = child.GetComponentInChildren<GameButton>(true) == null;
    float h = spacer ? EXIT_GAP : BTN_H;
    var le = child.GetComponent<LayoutElement>(); if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
    le.minHeight = le.preferredHeight = h; le.flexibleHeight = 0; le.preferredWidth = BTN_W; le.flexibleWidth = 0;
    EditorUtility.SetDirty(le);
    total += h;
    var btn = child.GetComponentInChildren<GameButton>(true);
    if (btn != null) {
        var label = btn.GetComponentInChildren<Text>(true);
        label.resizeTextForBestFit = false; label.fontSize = 25; if (agency != null) label.font = agency;
        label.alignment = TextAnchor.MiddleCenter;
        EditorUtility.SetDirty(label);
        sb.AppendLine($"  {btn.name}: hover={(btn.mouseOver ? btn.mouseOver.name : "none")} press={(btn.click ? btn.click.name : "none")}");
    }
}
total += GAP * (panel.Cast<Transform>().Count(t => t.gameObject.activeSelf) - 1);
panel.sizeDelta = new Vector2(BTN_W, total);
EditorUtility.SetDirty(panel); EditorUtility.SetDirty(vlg);

// Logo: left-aligned, same axis as the buttons, ruler underneath
var title = (RectTransform)root.Find("Title");
title.anchorMin = title.anchorMax = new Vector2(0, 0.5f); title.pivot = new Vector2(0, 0);
title.anchoredPosition = new Vector2(LEFT, COLUMN_TOP + TITLE_GAP); title.sizeDelta = new Vector2(700, TITLE_SIZE + 24);
var titleText = title.Find("Text").GetComponent<Text>();
var tt = (RectTransform)titleText.transform;
tt.anchorMin = new Vector2(0, 0); tt.anchorMax = new Vector2(1, 1); tt.pivot = new Vector2(0, 0.5f);
tt.offsetMin = new Vector2(0, 16); tt.offsetMax = Vector2.zero;
titleText.resizeTextForBestFit = false; titleText.fontSize = (int)TITLE_SIZE; titleText.alignment = TextAnchor.LowerLeft;
titleText.horizontalOverflow = HorizontalWrapMode.Overflow; titleText.verticalOverflow = VerticalWrapMode.Overflow;
if (agencyBold != null) titleText.font = agencyBold;
var ruler = (RectTransform)title.Find("Image");
ruler.anchorMin = ruler.anchorMax = new Vector2(0, 0); ruler.pivot = new Vector2(0, 0);
ruler.anchoredPosition = Vector2.zero; ruler.sizeDelta = new Vector2(BTN_W, 16);
EditorUtility.SetDirty(title); EditorUtility.SetDirty(tt); EditorUtility.SetDirty(titleText); EditorUtility.SetDirty(ruler);

EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
return $"font={(agency ? agency.name : "unchanged")} column {BTN_W}x{total} at x={LEFT}\n{sb}";
