using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentSelectionPanel : MonoBehaviour, SelectionManagerDelegate, TaskStatusUpdateDelegate, UserActionUpdateDelegate {

    Selection currentSelection;

    const string noSelectionText = "None";
    public Text title;

    public MasterAndGameTaskCell taskItemCell;
    public ActionsList actionsList;

    void Start() {
        title.text = noSelectionText;
        Script.Get<SelectionManager>().RegisterForNotifications(this);
    }

    private void OnDestroy() {
        Script.Get<SelectionManager>().EndNotifications(this);
    }

    /*
     * SelectionManagerDelegate Interface
     * */

    public void NotifyUpdateSelection(Selection nextSelection) {

        //if(currentSelection != null && currentSelection.selectionType == Selection.SelectionType.Selectable && currentSelection.selection is TaskStatusNotifiable) {
        //    (currentSelection.selection as TaskStatusNotifiable).EndNotifications(this);
        //}

        if (currentSelection != null) {
            currentSelection.EndSubscriptionToUserActions(this);
            currentSelection.EndSubscriptionToTaskStatus(this);
        }

        if (nextSelection != null) {
            title.text = nextSelection.Title();

            nextSelection.SubscribeToUserActions(this);
            nextSelection.SubscribeToTaskStatus(this);           
        } else {
            title.text = noSelectionText;
            actionsList.SetActions(new UserAction[] { });
        }

        currentSelection = nextSelection;
    }

    /*
     * TaskStatusUpdateDelegate Interface
     * */

    public void NowPerformingTask(MasterGameTask masterGameTask, GameTask gameTask) {
        taskItemCell.SetTask(masterGameTask, gameTask);
    }

    /*
     * UserActionUpdateDelegate Interface
     * */

    public void UpdateUserActionsAvailable(UserAction[] userActions) {
        actionsList.SetActions(userActions);
    }
}
