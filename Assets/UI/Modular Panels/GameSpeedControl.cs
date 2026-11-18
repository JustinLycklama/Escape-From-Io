using UnityEngine;
using UnityEngine.UI;

// Game speed (1x/2x/3x) for the top bar. Click cycles; keys 1-3 pick directly.
// Uses Time.timeScale, which is engine-global: reset to 1 on start and when this panel goes away (scene change).
// Pause is separate (PlayerBehaviour's paused flag), so speed and pause combine.
public class GameSpeedControl : MonoBehaviour, GameButtonDelegate, HotkeyDelegate {
    public GameButton speedButton;
    public Text label;

    private static readonly float[] speeds = { 1f, 2f, 3f };
    private static readonly KeyCode[] speedKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };

    private int speedIndex = 0;
    private PlayerBehaviour playerBehaviour;

    private void Awake() {
        speedButton.buttonDelegate = this;
    }

    private void Start() {
        playerBehaviour = Script.Get<PlayerBehaviour>();
        foreach (KeyCode key in speedKeys) {
            playerBehaviour.AddHotKeyDelegate(key, this);
        }

        SetSpeed(0);

        if (TutorialManager.isTutorial) {
            speedButton.SetEnabled(false);
        }
    }

    private void OnDestroy() {
        Time.timeScale = 1f;

        try {
            playerBehaviour.RemoveHotKeyDelegate(this);
        } catch (System.NullReferenceException) { }
    }

    public void SetSpeed(int index) {
        speedIndex = Mathf.Clamp(index, 0, speeds.Length - 1);
        Time.timeScale = speeds[speedIndex];
        label.text = $"{speeds[speedIndex]:0}x";
    }

    /*
     * GameButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        SetSpeed((speedIndex + 1) % speeds.Length);
    }

    /*
     * HotkeyDelegate Interface
     * */

    public void HotKeyPressed(KeyCode key) {
        int index = System.Array.IndexOf(speedKeys, key);
        if (index >= 0) {
            SetSpeed(index);
        }
    }
}
