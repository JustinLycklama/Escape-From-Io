// Play Mode input check (#8): queue real Input System device events across frames and log what the game did.
// Uses InputSystem.QueueStateEvent on Mouse.current/Keyboard.current (the same path as physical devices), because
// `unity command runtime simulate_pointer` dispatches UI events directly and never moves the Mouse device.
// Run in GameScene after init, then read the console for "[input-test]" lines (sequence takes ~2s).
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
using System.Reflection;

var player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
var selection = UnityEngine.Object.FindAnyObjectByType<SelectionManager>();
var selField = typeof(SelectionManager).GetField("currentSelection", BindingFlags.NonPublic | BindingFlags.Instance);

IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

void Mouse(Vector2 pos, MouseButton? held = null) {
    var state = new MouseState { position = pos };
    if (held.HasValue) state = state.WithButton(held.Value);
    InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current, state);
}

void Keys(params Key[] down) {
    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(down));
}

IEnumerator Sequence() {
    var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
    Mouse(center); yield return Frames(3);

    // Space toggles pause (and back)
    bool before = player.gamePaused;
    Keys(Key.Space); yield return Frames(2); Keys(); yield return Frames(3);
    bool afterOne = player.gamePaused;
    Keys(Key.Space); yield return Frames(2); Keys(); yield return Frames(3);
    Debug.Log($"[input-test] space: paused {before} -> {afterOne} -> {player.gamePaused}");

    // Hold W for ~0.5s real time: camera moves forward (+z), independent of time scale
    var cam = Camera.main.transform;
    var p0 = cam.position; float t0 = Time.realtimeSinceStartup;
    Keys(Key.W);
    while (Time.realtimeSinceStartup - t0 < 0.5f) yield return null;
    Keys(); yield return Frames(2);
    Debug.Log($"[input-test] W held 0.5s: dz={cam.position.z - p0.z:F1} (expected ~100 at 200 units/s)");

    // Middle-drag 200px to the right: camera moves left (-x) to follow the grabbed ground
    p0 = cam.position;
    Mouse(center, MouseButton.Middle); yield return Frames(2);
    for (int i = 1; i <= 10; i++) { Mouse(center + new Vector2(20 * i, 0), MouseButton.Middle); yield return Frames(1); }
    Mouse(center + new Vector2(200, 0)); yield return Frames(2);
    Debug.Log($"[input-test] middle-drag +200px: dx={cam.position.x - p0.x:F1} (expected ~-120)");

    // Left click on the terrain under the screen centre selects it
    Mouse(center); yield return Frames(2);
    Mouse(center, MouseButton.Left); yield return Frames(2);
    Mouse(center); yield return Frames(3);
    var sel = selField.GetValue(selection);
    Debug.Log($"[input-test] left click centre: selection={(sel == null ? "none" : sel.ToString())}");

    // Scroll one notch: camera zooms (y changes)
    var y0 = cam.position.y;
    InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current, new MouseState { position = center, scroll = new Vector2(0, 120) });
    yield return Frames(2);
    Mouse(center); yield return Frames(2);
    Debug.Log($"[input-test] scroll one notch: dy={cam.position.y - y0:F2}");
}

player.StartCoroutine(Sequence());
return "input sequence started";
