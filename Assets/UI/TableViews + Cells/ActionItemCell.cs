using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UserAction {
    public string description;

    public Action performAction;
}

public class ActionItemCell : MonoBehaviour, IPointerClickHandler {
    public Text actionItemTitle;
    UserAction action;

    const string defaultText = " - ";

    private void Awake() {
        actionItemTitle.text = defaultText;
    }

    public void SetAction(UserAction action) {
        this.action = action;

        if (action == null) {
            actionItemTitle.text = defaultText;
        } else {
            actionItemTitle.text = action.description;
        }   
    }

    /*
   * IPointerClickHandler Interface
   * */

    public void OnPointerClick(PointerEventData eventData) {
        if(action == null) {
            return;
        }

        action.performAction();
    }
}
