using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPanel : MonoBehaviour, GameButtonDelegate
{
    public GameButton closeButton;

    public GameButton basicButton;
    public GameButton defendeseButton;
    public GameButton escapeButton;

    public TitleWindow titleController;
    public FadePanel fadePanel;

    // Start is called before the first frame update
    void Start()
    {
        foreach(GameButton button in new GameButton[] { closeButton, basicButton, defendeseButton, escapeButton }) {
            button.buttonDelegate = this;
        }
    }

    public void ButtonDidClick(GameButton button) {
        if(button == closeButton) {
            titleController.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
