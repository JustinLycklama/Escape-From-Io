using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuWindow : MonoBehaviour, ButtonDelegate, CanSceneChangeDelegate {
    public GameButton continueButton;
    public GameButton restartButton;
    public GameButton helpButton;
    public GameButton quitButton;

    public FadePanel fadePanel;
    public SettingsPanel settingsPanel;


    private bool ableToSwitchScene = false;

    private void Start() {
        foreach(GameButton button in new GameButton[] { continueButton, restartButton, helpButton, quitButton }) {
            button.buttonDelegate = this;
        }
    }
        
   /*
    * ButtonDelegate Interface
    * */

    public void ButtonDidClick(GameButton button) {

        Action completeTransition = () => {
            ableToSwitchScene = true;
        };

        if (button == continueButton) {
            settingsPanel.CloseWindows();
        } else if(button == restartButton) {
            settingsPanel.CloseWindows();
            fadePanel.FadeOut(true, completeTransition);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.NewGame, null, null, this);
        } else if(button == helpButton) {

        } else if(button == quitButton) {
            settingsPanel.CloseWindows();
            fadePanel.FadeOut(true, completeTransition);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.Title, null, null, this);
        }
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return ableToSwitchScene;
    }
}
