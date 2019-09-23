using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UserAction {
    public string description;
    public LayoutCoordinate layoutCoordinate;

    // We can have a single action to perform, or a list of construction blueprints which will contain their own actions
    public Action<LayoutCoordinate> performAction;

    public ConstructionBlueprint[] blueprintList;
}

public class ActionItemCell : Clickable, HotkeyDelegate {
    public Text actionItemTitle;
    UserAction action;
    KeyCode? hotkey;

    const string defaultText = " - ";

    protected override void Awake() {
        base.Awake();

        actionItemTitle.text = defaultText;
    }

    public void SetAction(UserAction action) {
        this.action = action;

        UpdateText();
    }

    public void SetHotKey(KeyCode hotkey) {
        PlayerBehaviour playerBehaviour = Script.Get<PlayerBehaviour>();

        if(this.hotkey != null) {
            playerBehaviour.RemoveHotKeyDelegate(this);
        }

        this.hotkey = hotkey;
        playerBehaviour.AddHotKeyDelegate(hotkey, this);

        UpdateText();
    }

    private void UpdateText() {
        string hotkeyText = "";

        if (hotkey != null) {
            hotkeyText = " (" + hotkey.ToString() + ")";
        }

        if(action == null) {
            actionItemTitle.text = defaultText;
        } else {
            actionItemTitle.text = action.description + hotkeyText;
        }
    }

    /*
    * Clickable Overrides
    * */

    protected override void DidClick() {
        if(action == null) {
            return;
        }

        // We either have an action to perform, or a list of blueprints to display
        if(action.performAction != null) {
            action.performAction(action.layoutCoordinate);
        } else if(action.blueprintList != null) {
            BlueprintPanel blueprintPanel = Script.Get<UIManager>().Push(UIManager.Blueprint.BlueprintPanel) as BlueprintPanel;
            blueprintPanel.SetData(action.description, action.blueprintList, action.layoutCoordinate);
        }
    }

    public void HotKeyPressed(KeyCode vKey) {
        DidClick();
    }
}
