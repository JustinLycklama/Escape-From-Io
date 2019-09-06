using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsPanel : MonoBehaviour, PlayerBehaviourUpdateDelegate, ButtonDelegate {
    public GameButton settingsButton;
    public GameButton playButton;
    public GameButton pauseButton;

    public MenuWindow menuWindow;

    private PlayerBehaviour playerBehaviour;
    private bool lastPausedState = false;


    private void Awake() {
        settingsButton.buttonDelegate = this;
        playButton.buttonDelegate = this;
        pauseButton.buttonDelegate = this;
    }

    private void Start() {
        playerBehaviour = Script.Get<PlayerBehaviour>();

        playerBehaviour.RegisterForPlayerBehaviourNotifications(this);
        playerBehaviour.SetPauseState(false);

        menuWindow.gameObject.SetActive(false);
    }

    private void OnDestroy() {
        try {
            playerBehaviour.EndPlayerBehaviourNotifications(this);
        } catch(System.NullReferenceException e) { }
    }

    public void CloseWindows() {
        menuWindow.gameObject.SetActive(false);
        playerBehaviour.SetPauseState(lastPausedState);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if(button == settingsButton) {
            lastPausedState = playerBehaviour.gamePaused;
            playerBehaviour.SetPauseState(true);
            menuWindow.gameObject.SetActive(true);
        } else if(button == playButton) {
            playerBehaviour.SetPauseState(false);
        } else if(button == pauseButton) {
            playerBehaviour.SetPauseState(true);
        }
    }

    /*    
    * PlayerBehaviourUpdateDelegate Interface           
    * */

    public void PauseStateUpdated(bool paused) {
        playButton.SetHoverLock(!paused);
        pauseButton.SetHoverLock(paused);
    }
}