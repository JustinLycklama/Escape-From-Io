using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentSelectionPanel : MonoBehaviour, SelectionManagerDelegate, TaskStatusUpdateDelegate {

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

        if(currentSelection != null && currentSelection.selectionType == Selection.SelectionType.Selectable && currentSelection.selection is TaskStatusNotifiable) {
            (currentSelection.selection as TaskStatusNotifiable).EndNotifications(this);
        }

        if (nextSelection != null) {
            title.text = nextSelection.Title();
            actionsList.SetActions(nextSelection.UserActions());

            if (nextSelection.selectionType == Selection.SelectionType.Selectable && nextSelection.selection is TaskStatusNotifiable) {
                (nextSelection.selection as TaskStatusNotifiable).RegisterForNotifications(this);
            }            
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
}
