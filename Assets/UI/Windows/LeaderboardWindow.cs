using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardWindow : MonoBehaviour, ButtonDelegate {
    public GameButton closeButton;
    public GameButton replayButton;

    public TitleWindow titleController;

    private void Awake() {
        closeButton.buttonDelegate = this;
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if(button == closeButton) {
            titleController.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
