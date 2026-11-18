// Editor eval (#18): permanent minimap bottom-right. Moves the map panel out of the bottom-left slide-out (OverviewPanel),
// docks it at the bottom-right (220x220 reference units), adds click/drag-to-pan, and retires the slide-out. Safe to re-run.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

const float MARGIN = 20, SIZE = 220;
var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay);
var guiOverlay = canvas.transform.Find("GUI Overlay Panel");
var miniMap = Object.FindAnyObjectByType<MiniMap>(FindObjectsInactive.Include);
var mapPanel = miniMap.transform.parent;   // MapPanel (frame image + aspect fitter)
var overview = Object.FindAnyObjectByType<OverviewPanel>(FindObjectsInactive.Include);

var dock = guiOverlay.Find("MinimapDock") as RectTransform;
if (dock == null) {
    var go = new GameObject("MinimapDock", typeof(RectTransform));
    go.layer = LayerMask.NameToLayer("UI");
    dock = (RectTransform)go.transform;
    dock.SetParent(guiOverlay, false);
}
dock.anchorMin = dock.anchorMax = new Vector2(1, 0); dock.pivot = new Vector2(1, 0);
dock.anchoredPosition = new Vector2(-MARGIN, MARGIN); dock.sizeDelta = new Vector2(SIZE, SIZE);

mapPanel.SetParent(dock, false);
var mp = (RectTransform)mapPanel;
var fitter = mapPanel.GetComponent<AspectRatioFitter>(); if (fitter != null) Object.DestroyImmediate(fitter);
mp.anchorMin = Vector2.zero; mp.anchorMax = Vector2.one; mp.pivot = new Vector2(0.5f, 0.5f); mp.offsetMin = mp.offsetMax = Vector2.zero;
EditorUtility.SetDirty(mp);

var frame = mapPanel.GetComponent<Image>(); frame.raycastTarget = true;
var nav = mapPanel.GetComponent<MiniMapNavigation>(); if (nav == null) nav = mapPanel.gameObject.AddComponent<MiniMapNavigation>();
nav.miniMap = miniMap;

// The slide-out now holds nothing but its toggle
overview.gameObject.SetActive(false);

EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Canvas.ForceUpdateCanvases();
return $"minimap docked bottom-right ({mp.rect.width:0}x{mp.rect.height:0}), click/drag to pan; OverviewPanel retired";
