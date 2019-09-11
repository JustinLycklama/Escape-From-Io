using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageWindow : MonoBehaviour, GameButtonDelegate {

    public Text title;
    public Text detailText;

    public GameButton button1;
    public Text button1Title;
    public GameButton button2;
    public Text button2Title;

    private Action action1;
    private Action action2;

    private void Start() {
        foreach(GameButton button in new GameButton[] { button1, button2 }) {
            button.buttonDelegate = this;
        }
    }

    public void Display() {
        Script.Get<PlayerBehaviour>().SetMenuPause(true);

        GameObject uiManager = Tag.UIManager.GetGameObject();
        transform.SetParent(uiManager.transform, false);      
    }

    public void SetTitleAndText(string title, string text) {
        this.title.text = title;
        this.detailText.text = text;
    }

    public void SetSingleAction(Action action1, string title) {
        button1.gameObject.SetActive(true);
        button2.gameObject.SetActive(false);

        button1Title.text = title;
        this.action1 = action1;
    }

    public void SetDoubleAction(Action action1, string title1, Action action2, string title2) {
        button1.gameObject.SetActive(true);
        button2.gameObject.SetActive(true);

        button1Title.text = title1;
        this.action1 = action1;

        button2Title.text = title2;
        this.action2 = action2;
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        Script.Get<PlayerBehaviour>().SetMenuPause(false);

        if(button == button1 && action1 != null) {
            action1();
        } else if(button == button2 && action2 != null) {
            action2();
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
