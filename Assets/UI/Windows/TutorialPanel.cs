using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPanel : MonoBehaviour, GameButtonDelegate, CanSceneChangeDelegate {
    public GameButton closeButton;

    public GameButton basicButton;
    public GameButton defendeseButton;
    public GameButton escapeButton;

    public TitleWindow titleController;
    public FadePanel fadePanel;

    bool ableToSwitchScene = false;

    // Start is called before the first frame update
    void Start()
    {
        foreach(GameButton button in new GameButton[] { closeButton, basicButton, defendeseButton, escapeButton }) {
            button.buttonDelegate = this;
        }
    }

    public void ButtonDidClick(GameButton button) {
        Action completeTransition = () => {
            ableToSwitchScene = true;
        };

        if(button == closeButton) {
            titleController.gameObject.SetActive(true);
            gameObject.SetActive(false);
        } else {

            if(button == basicButton) {
                TutorialManager.sharedInstance.tutorialType = TutorialType.Basic;
            } else if(button == defendeseButton) {
                TutorialManager.sharedInstance.tutorialType = TutorialType.Defense;
            } else if(button == escapeButton) {
                TutorialManager.sharedInstance.tutorialType = TutorialType.Escape;
            }

            fadePanel.FadeOut(true, true, completeTransition);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.Tutorial, null, null, this);
        }
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return ableToSwitchScene;
    }
}
