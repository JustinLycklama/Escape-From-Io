// Editor eval (#15): build the HUD top bar in GameScene and move the existing, already-wired pieces into it.
//   left:   ResourcesPanel
//   centre: play, pause (SettingsPanel), new speed button (GameSpeedControl), clock (TimePanel)
//   right:  threat (AttackersCell, without its old background frame), settings gear
// Sizes are 1600x900 reference units (see ARCHITECTURE.md -> UI style guide). Safe to re-run.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Linq;

const float BAR_HEIGHT = 52, MARGIN = 20, PAD = 12, BUTTON = 40, GAP = 8;
var sb = new StringBuilder();
var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

var settings = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
var time = Object.FindAnyObjectByType<TimePanel>(FindObjectsInactive.Include);
var threat = Object.FindAnyObjectByType<AttackersCell>(FindObjectsInactive.Include);
var resources = Object.FindAnyObjectByType<ResourcesPanel>(FindObjectsInactive.Include);
var overview = Object.FindAnyObjectByType<OverviewPanel>(FindObjectsInactive.Include);
var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
    .First(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay);
var guiOverlay = canvas.transform.Find("GUI Overlay Panel");
var agency = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Assets/Fonts/AgencyFB-Bold.otf");

RectTransform NewRect(string name, Transform parent) {
    var go = new GameObject(name, typeof(RectTransform));
    go.layer = LayerMask.NameToLayer("UI");
    var rt = (RectTransform)go.transform;
    rt.SetParent(parent, false);
    return rt;
}

void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) {
    rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
}

HorizontalLayoutGroup Row(RectTransform rt, TextAnchor align) {
    var h = rt.GetComponent<HorizontalLayoutGroup>();
    if (h == null) h = rt.gameObject.AddComponent<HorizontalLayoutGroup>(); // Unity fake-null: no ?? on components
    h.spacing = GAP; h.childAlignment = align;
    h.childControlWidth = h.childControlHeight = true;
    h.childForceExpandWidth = h.childForceExpandHeight = false;
    h.padding = new RectOffset(0, 0, 0, 0);
    return h;
}

void Size(Component c, float w, float h) {
    var le = c.GetComponent<LayoutElement>();
    if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
    le.minWidth = le.preferredWidth = w; le.minHeight = le.preferredHeight = h;
    le.flexibleWidth = le.flexibleHeight = 0;
}

// Children of a prefab instance can't be re-parented (Unity silently refuses), so unpack the HUD panels that
// hold these pieces (TaskAndUnitPanel, OverviewPanel). Loop because nested prefabs unpack one level at a time.
void Unpack(Component c) {
    while (PrefabUtility.IsPartOfPrefabInstance(c.gameObject) && !PrefabUtility.IsOutermostPrefabInstanceRoot(c.gameObject)) {
        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject);
        sb.AppendLine($"unpacking prefab instance '{root.name}'");
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
    }
}
foreach (var c in new Component[] { settings, time, threat, resources, settings.settingsButton }) Unpack(c);

// Re-run safety: move every piece out of any existing bar (including nested ones), then delete the bars
var gearPanelForRescue = settings.settingsButton.transform.parent;
var oldBars = canvas.GetComponentsInChildren<Transform>(true).Where(t => t.name == "TopBar").ToList();
if (oldBars.Count > 0) {
    foreach (var c in new Component[] { settings, time, threat, resources, gearPanelForRescue }) c.transform.SetParent(guiOverlay, false);
    foreach (var b in oldBars) if (b != null) Object.DestroyImmediate(b.gameObject);
    sb.AppendLine($"removed {oldBars.Count} previous TopBar(s)");
}

// Bar
var bar = NewRect("TopBar", guiOverlay);
bar.SetSiblingIndex(0);
bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
bar.offsetMin = new Vector2(MARGIN, -MARGIN - BAR_HEIGHT); bar.offsetMax = new Vector2(-MARGIN, -MARGIN);
var bg = bar.gameObject.AddComponent<Image>();
bg.color = new Color(0.02f, 0.06f, 0.12f, 0.78f);
bg.raycastTarget = true;

// Left: resources
var left = NewRect("Left", bar);
Place(left, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(PAD, 0), new Vector2(360, 34));
resources.transform.SetParent(left, false);
var resRt = (RectTransform)resources.transform;
resRt.anchorMin = Vector2.zero; resRt.anchorMax = Vector2.one; resRt.pivot = new Vector2(0.5f, 0.5f);
resRt.offsetMin = resRt.offsetMax = Vector2.zero;

// Centre: play/pause (+ speed) then clock
var centre = NewRect("Centre", bar);
Place(centre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, BUTTON));
Row(centre, TextAnchor.MiddleCenter);

// SettingsPanel keeps play + pause (and the new speed button); its settings gear moves right
var gearPanel = settings.settingsButton.transform.parent;
settings.transform.SetParent(centre, false);
Row((RectTransform)settings.transform, TextAnchor.MiddleCenter);
Size(settings, BUTTON * 3 + GAP * 2, BUTTON);

foreach (Transform panel in settings.transform) Size(panel, BUTTON, BUTTON);

// Speed button: copy of the gear button with a text label instead of the icon
var existingSpeed = settings.transform.Find("SpeedPanel");
if (existingSpeed != null) Object.DestroyImmediate(existingSpeed.gameObject);
var speedPanel = Object.Instantiate(gearPanel.gameObject, settings.transform);
speedPanel.name = "SpeedPanel";
var speedButton = speedPanel.GetComponentInChildren<GameButton>(true);
speedButton.name = "SpeedButton";
foreach (Transform child in speedButton.transform) Object.DestroyImmediate(child.gameObject);
var labelRt = NewRect("Label", speedButton.transform);
labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one; labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
var label = labelRt.gameObject.AddComponent<Text>();
label.font = agency; label.fontSize = 18; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.text = "1x";
label.raycastTarget = false;
var speedControl = speedPanel.AddComponent<GameSpeedControl>();
speedControl.speedButton = speedButton; speedControl.label = label;
Size(speedPanel.transform, BUTTON, BUTTON);

time.transform.SetParent(centre, false);
Size(time, 120, 34);

// Right: threat then settings gear
var right = NewRect("Right", bar);
Place(right, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-PAD, 0), new Vector2(400, BAR_HEIGHT));
Row(right, TextAnchor.MiddleRight);
threat.transform.SetParent(right, false);
var threatBg = threat.transform.Find("Background");
if (threatBg != null) threatBg.gameObject.SetActive(false);
Size(threat, 352, BAR_HEIGHT);
gearPanel.SetParent(right, false);
Size(gearPanel, BUTTON, BUTTON);

// The robot panel lost the threat cell from its top; keep its rows at their previous height (#16 replaces this panel)
var robotPanel = Object.FindAnyObjectByType<NavigationPanel>(FindObjectsInactive.Include);
if (robotPanel != null && robotPanel.name == "TaskAndUnitPanel") {
    var rp = (RectTransform)robotPanel.transform;
    rp.sizeDelta = new Vector2(rp.sizeDelta.x, 420);
}

// The overview slide-out keeps only its toggle and the minimap now
var menuElement = overview.transform.Find("Menu Element");
if (menuElement != null) menuElement.gameObject.SetActive(false);

EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
sb.AppendLine("TopBar built: left=resources, centre=play/pause/speed + clock, right=threat + settings");
return sb.ToString();
