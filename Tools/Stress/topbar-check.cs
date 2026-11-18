// Play Mode check (#15): keys 2/3/1 set game speed, the speed button cycles it, Space pauses; logs "[topbar-test]" lines.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;

var player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>();
var speed = UnityEngine.Object.FindAnyObjectByType<GameSpeedControl>();

IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
IEnumerator Tap(Key k) {
    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(k)); yield return Frames(2);
    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return Frames(3);
}

IEnumerator Run() {
    Debug.Log($"[topbar-test] start: timeScale={Time.timeScale} label={speed.label.text} paused={player.gamePaused}");
    yield return Tap(Key.Digit2); Debug.Log($"[topbar-test] key 2: timeScale={Time.timeScale} label={speed.label.text}");
    yield return Tap(Key.Digit3); Debug.Log($"[topbar-test] key 3: timeScale={Time.timeScale} label={speed.label.text}");
    speed.ButtonDidClick(speed.speedButton); yield return Frames(2);
    Debug.Log($"[topbar-test] button click (3x wraps): timeScale={Time.timeScale} label={speed.label.text}");
    yield return Tap(Key.Space); Debug.Log($"[topbar-test] space: paused={player.gamePaused} timeScale={Time.timeScale}");
    yield return Tap(Key.Space); Debug.Log($"[topbar-test] space again: paused={player.gamePaused}");
    yield return Tap(Key.Digit1); Debug.Log($"[topbar-test] key 1: timeScale={Time.timeScale} label={speed.label.text}");
}
player.StartCoroutine(Run());
return "top bar check started";
