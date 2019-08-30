using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsPanel : MonoBehaviour, PlayerBehaviourUpdateDelegate, ButtonDelegate {
    public GameButton settingsButton;
    public GameButton playButton;
    public GameButton pauseButton;

    private PlayerBehaviour playerBehaviour;

    private void Awake() {
        settingsButton.buttonDelegate = this;
        playButton.buttonDelegate = this;
        pauseButton.buttonDelegate = this;
    }

    private void Start() {
        playerBehaviour = Script.Get<PlayerBehaviour>();

        playerBehaviour.RegisterForPlayerBehaviourNotifications(this);
        playerBehaviour.SetPauseState(false);
    }

    private void OnDestroy() {
        playerBehaviour.EndPlayerBehaviourNotifications(this);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if(button == settingsButton) {

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