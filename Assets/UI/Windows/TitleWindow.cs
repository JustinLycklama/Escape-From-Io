using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleWindow : MonoBehaviour, ButtonDelegate, CanSceneChangeDelegate {
    public GameButton tutorial;
    public GameButton newGame;
    public GameButton leaderboard;
    public GameButton exit;

    public LeaderboardWindow leaderboardPanel;
    public FadePanel fadePanel;

    bool ableToSwitchScene = false;

    private void Awake() {
        foreach(GameButton button in new GameButton[] { tutorial, newGame, leaderboard, exit } ) {
            button.buttonDelegate = this;
        }
    }

    private void Start() {
        leaderboardPanel.gameObject.SetActive(false);
        fadePanel.FadeOut(false, null);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {       
        Action completeTransition = () => {
            ableToSwitchScene = true;
        };

        if(button == tutorial) {
            fadePanel.FadeOut(true, completeTransition);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.Tutorial, null, null, this);
        } else if(button == newGame) {

        } else if(button == leaderboard) {
            leaderboardPanel.gameObject.SetActive(true);
            gameObject.SetActive(false);
        } else if(button == exit) {

        }
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return ableToSwitchScene;
    }
}
