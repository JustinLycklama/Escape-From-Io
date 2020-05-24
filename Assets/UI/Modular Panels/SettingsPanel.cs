using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsPanel : MonoBehaviour, PlayerBehaviourUpdateDelegate, GameButtonDelegate, HotkeyDelegate {
    public GameButton settingsButton;
    public GameButton playButton;
    public GameButton pauseButton;

    public MenuWindow menuWindow;

    private PlayerBehaviour playerBehaviour;

    private void Awake() {
        settingsButton.buttonDelegate = this;
        playButton.buttonDelegate = this;
        pauseButton.buttonDelegate = this;
    }

    private void Start() {
        playerBehaviour = Script.Get<PlayerBehaviour>();
        playerBehaviour.RegisterForPlayerBehaviourNotifications(this);
        playerBehaviour.AddHotKeyDelegate(KeyCode.Escape, this);

        menuWindow.gameObject.SetActive(false);

        if (TutorialManager.isTutorial) {
            playButton.SetEnabled(false);
            pauseButton.SetEnabled(false);
        }
    }

    private void OnDestroy() {
        try {
            playerBehaviour.EndPlayerBehaviourNotifications(this);
            playerBehaviour.RemoveHotKeyDelegate(this);
        } catch(System.NullReferenceException) { }
    }

    public void CloseWindows() {
        menuWindow.gameObject.SetActive(false);
        playerBehaviour.SetInternalPause(false);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if(button == settingsButton) {
            playerBehaviour.SetInternalPause(true);
            menuWindow.gameObject.SetActive(true);
        } else if(button == playButton) {
            playerBehaviour.SetPlayerPauseState(false);
        } else if(button == pauseButton) {
            playerBehaviour.SetPlayerPauseState(true);
        }
    }

    /*    
    * PlayerBehaviourUpdateDelegate Interface           
    * */

    public void PauseStateUpdated(bool paused) {
        playButton.SetHoverLock(!paused);
        pauseButton.SetHoverLock(paused);
    }

    /*
     * HotkeyDelegate Interface
     * */

    public void HotKeyPressed(KeyCode key) {
        ButtonDidClick(settingsButton);
    }
}