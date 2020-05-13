using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameMessagePanel : MonoBehaviour, GameButtonDelegate, IPointerClickHandler {
    [SerializeField]
    private Text title = null;
    [SerializeField]
    private Text contents = null;

    [SerializeField]
    public GameButton continueButton;

    [HideInInspector]
    public GameButtonDelegate buttonDelegate;

    private void Start() {
        continueButton.buttonDelegate = this;
    }

    public void SetTitleAndText(string titleText, string message) {
        title?.gameObject.SetActive(titleText != null);

        if(title != null) {
            title.text = titleText;
        }

        contents.text = message;
    }

    public void ButtonDidClick(GameButton button) {
        buttonDelegate.ButtonDidClick(button);
    }

    public void OnPointerClick(PointerEventData eventData) {
        buttonDelegate.ButtonDidClick(continueButton);
    }
}

