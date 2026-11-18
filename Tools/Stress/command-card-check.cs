// Play Mode check (#17): select the tile near spawn with the most actions, then click its "Build ..." action (if any)
// through the EventSystem to open the BlueprintPanel. Logs "[cmd-test]" lines with what's visible.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections;

var selection = UnityEngine.Object.FindAnyObjectByType<SelectionManager>();
var mapGen = UnityEngine.Object.FindAnyObjectByType<MapGenerator>();
var terrain = UnityEngine.Object.FindAnyObjectByType<TerrainManager>();
var constants = UnityEngine.Object.FindAnyObjectByType<Constants>();
var player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
var s = mapGen.spawnCoordinate;

LayoutCoordinate best = s; int bestCount = -1;
for (int x = 0; x < constants.layoutMapWidth; x++) for (int y = 0; y < constants.layoutMapHeight; y++) {
    var c = new LayoutCoordinate(x, y, s.mapContainer);
    if (c.x == s.x && c.y == s.y) continue;
    int n = terrain.ActionsAvailableForTerrain(c)?.Length ?? 0;
    if (n > bestCount) { best = c; bestCount = n; }
}
selection.SelectTerrain(best);

IEnumerator Run() {
    yield return null; yield return null;
    var panel = UnityEngine.Object.FindAnyObjectByType<CurrentSelectionPanel>();
    var labels = panel.GetComponentsInChildren<ActionItemCell>().Select(c => c.GetComponentInChildren<Text>().text).ToArray();
    Debug.Log($"[cmd-test] tile '{best.mapContainer.map.GetTerrainAt(best).name}' actions shown: {string.Join(" | ", labels)}");

    var build = panel.GetComponentsInChildren<ActionItemCell>().FirstOrDefault(c => c.GetComponentInChildren<Text>().text.StartsWith("Build"));
    if (build == null) { Debug.Log("[cmd-test] no Build action on this tile"); yield break; }
    ExecuteEvents.Execute(build.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
    yield return null; yield return null;
    var bp = UnityEngine.Object.FindAnyObjectByType<BlueprintPanel>();
    var rt = bp ? (RectTransform)bp.transform : null;
    Debug.Log(bp ? $"[cmd-test] BlueprintPanel open: anchor={rt.anchorMin} pivot={rt.pivot} pos={rt.anchoredPosition} size={rt.rect.size}" : "[cmd-test] BlueprintPanel did not open");
}
player.StartCoroutine(Run());
return $"selected tile with {bestCount} actions";
