using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserAction {
    public string description;

    public Action performAction;
}

public class ActionItemCell : MonoBehaviour
{
    public Text actionItemTitle;
    public Button performAction;

    UserAction action;

    private void Start() {
        performAction.onClick.AddListener(ButtonPress);
    }

    public void SetAction(UserAction action) {
        this.action = action;

        actionItemTitle.text = action.description;
    }

    private void ButtonPress() {
        if (action == null) {
            return;
        }

        action.performAction();
    }
}
