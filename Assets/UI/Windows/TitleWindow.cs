using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleWindow : MonoBehaviour, GameButtonDelegate, CanSceneChangeDelegate, HelpPresenter {
    public GameButton tutorial;
    public GameButton newGame;
    public GameButton leaderboard;
    public GameButton exit;

    public LeaderboardWindow leaderboardPanel;
    public HelpWindow helpWindow;
    public FadePanel fadePanel;

    bool ableToSwitchScene = false;

    private void Awake() {
        foreach(GameButton button in new GameButton[] { tutorial, newGame, leaderboard, exit } ) {
            button.buttonDelegate = this;
        }
    }

    private void Start() {
        leaderboardPanel.gameObject.SetActive(false);
        helpWindow.gameObject.SetActive(false);
        fadePanel.FadeOut(false, null);

        helpWindow.presenter = this;        

        if (SceneManagement.sharedInstance.state == SceneManagement.State.GameFinish) {
            ButtonDidClick(leaderboard);
        }
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {       
        Action completeTransition = () => {
            ableToSwitchScene = true;
        };

        if(button == tutorial) {
            //helpWindow.gameObject.SetActive(true);
            //gameObject.SetActive(false);

            fadePanel.FadeOut(true, completeTransition);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.Tutorial, null, null, this);
        
        } else if(button == newGame) {
            fadePanel.FadeOut(true, completeTransition);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.NewGame, null, null, this);
        } else if(button == leaderboard) {
            leaderboardPanel.gameObject.SetActive(true);
            gameObject.SetActive(false);
        } else if(button == exit) {
            Application.Quit();
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
