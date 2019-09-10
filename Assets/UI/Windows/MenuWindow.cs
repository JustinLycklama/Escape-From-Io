using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuWindow : MonoBehaviour, ButtonDelegate, CanSceneChangeDelegate, HelpPresenter {
    public GameButton continueButton;
    public GameButton restartButton;
    public GameButton helpButton;
    public GameButton quitButton;

    public FadePanel fadePanel;
    public SettingsPanel settingsPanel;

    public HelpWindow helpWindow;

    private bool ableToSwitchScene = false;

    private void Start() {
        foreach(GameButton button in new GameButton[] { continueButton, restartButton, helpButton, quitButton }) {
            button.buttonDelegate = this;
        }

        helpWindow.gameObject.SetActive(false);
        helpWindow.presenter = this;
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
            gameObject.SetActive(false);
            helpWindow.gameObject.SetActive(true);
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

    /*
     * HelpPresenter Interface
     * */

    public void dismiss(HelpWindow window) {
        gameObject.SetActive(true);
        window.gameObject.SetActive(false);
    }
}
