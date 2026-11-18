// Play Mode probe for input checks (#8): camera position, pause state, current selection, screen size, active scene.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Reflection;

var cam = Camera.main;
var player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
var selection = UnityEngine.Object.FindAnyObjectByType<SelectionManager>();
object selected = null;
if (selection != null) {
    var f = typeof(SelectionManager).GetField("currentSelection", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
    selected = f?.GetValue(selection);
}
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
return $"scene={scene} screen={Screen.width}x{Screen.height} cam={(cam ? cam.transform.position.ToString("F1") : "none")} paused={player?.gamePaused} selection={(selected == null ? "none" : selected.ToString())}";
