using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardWindow : MonoBehaviour, GameButtonDelegate, CanSceneChangeDelegate {
    public GameButton closeButton;
    public GameButton replayButton;

    public TitleWindow titleController;
    public FadePanel fadePanel;

    public FirebaseManager firebaseManager;
    public HighscoreController highscoreController;

    private bool ableToSwitchScene = false;

    private void Awake() {
        closeButton.buttonDelegate = this;
        replayButton.buttonDelegate = this;
    }

    private void Start() {
        if (SceneManagement.sharedInstance.state != SceneManagement.State.GameFinish) {
            replayButton.gameObject.SetActive(false);
        }

        firebaseManager.firebaseDelegate = highscoreController;
        firebaseManager.ReadScore();
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        Action completeTransition = () => {
            ableToSwitchScene = true;
        };

        if(button == closeButton) {
            titleController.gameObject.SetActive(true);
            gameObject.SetActive(false);
        } else if (button == replayButton) {
            fadePanel.FadeOut(true, false, completeTransition);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.NewGame, null, null, this);
        }
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return ableToSwitchScene;
    }
}
