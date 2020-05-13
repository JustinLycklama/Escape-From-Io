using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UserAction {
    public enum UserActionTutorialIdentifier {
        None,
        DrillDown,
        BuildUnit,
        BuildBuilding,
        Mine,
        Clean,
        Path
    }

    public UserActionTutorialIdentifier tutorialIdentifier = UserActionTutorialIdentifier.None;

    public string description;
    public LayoutCoordinate layoutCoordinate;

    public List<MasterGameTask.ActionType> associatedActionTypes = new List<MasterGameTask.ActionType>();

    // We can have a single action to perform, or a list of construction blueprints which will contain their own actions
    public Action<LayoutCoordinate> performAction;

    public ConstructionBlueprint[] blueprintList;

    public bool shouldDeselectAfterAction = true;
}

public class ActionItemCell : Clickable, HotkeyDelegate {
    [SerializeField]
    private Text actionItemTitle;

    [SerializeField]
    List<GameObject> unitIconContainers;

    [SerializeField]
    List<UnitTypeIcon> unitIcons;

    //[SerializeField]
    //private GameObject unitIconContainer;

    private PlayerBehaviour playerBehaviour;

    private UserAction action;
    private KeyCode? hotkey;

    const string defaultText = " - ";

    protected override void Awake() {
        base.Awake();

        actionItemTitle.text = defaultText;
        playerBehaviour = Script.Get<PlayerBehaviour>();
    }

    private void OnDestroy() {
        playerBehaviour?.RemoveHotKeyDelegate(this);
    }

    public void SetAction(UserAction action) {
        this.action = action;

        List<MasterGameTask.ActionType> associatedTypes;

        if (action == null) {
            associatedTypes = new List<MasterGameTask.ActionType>();
        } else {
            associatedTypes = action.associatedActionTypes;
        }

        for (int i = 0; i < unitIcons.Count; i++) {
            bool unitIconActive = (associatedTypes.Count > i);

            GameObject unitIconContainer = unitIconContainers[i];

            if(unitIconContainer.gameObject.activeSelf != unitIconActive) {
                unitIconContainer.gameObject.SetActive(unitIconActive);
            }

            UnitTypeIcon unitIcon = unitIcons[i];

            if (unitIconActive) {
                unitIcon.SetActionType(associatedTypes[i]);
            }
        }

        UpdateButtonState();
    }

    public void SetHotKey(KeyCode hotkey) {

        if(this.hotkey != null) {
            playerBehaviour.RemoveHotKeyDelegate(this);
        }

        this.hotkey = hotkey;
        playerBehaviour.AddHotKeyDelegate(hotkey, this);

        UpdateButtonState();
    }

    private void UpdateButtonState() {
        string hotkeyText = "";

        if (hotkey != null) {
            hotkeyText = " (" + hotkey.ToString() + ")";
        }

        if(action == null) {
            actionItemTitle.text = defaultText;
        } else {
            actionItemTitle.text = action.description + hotkeyText;
        }


        bool enabled = action != null;
        var currentTutorialIdentifier = TutorialManager.isolateUserAction;

        if (currentTutorialIdentifier != null) {
            enabled = enabled && currentTutorialIdentifier.userActionIdentifier == action.tutorialIdentifier;
        }

        if(buttonEnabled != enabled) {
            SetEnabled(enabled);
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
            if(action.shouldDeselectAfterAction) {
                Script.Get<SelectionManager>().RemoveSelection();
            }

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
