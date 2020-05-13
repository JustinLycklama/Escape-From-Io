using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentSelectionPanel : NavigationPanel, SelectionManagerDelegate, TaskStatusUpdateDelegate, UserActionUpdateDelegate, MasterTaskUpdateDelegate {


    private Selection currentSelection;

    const string noSelectionText = "None";

    [SerializeField]
    private Text titleText = null;

    [SerializeField]
    private CanvasGroup infoAreaCanvas = null;

    [SerializeField]
    private MasterAndGameTaskCell taskItemCell = null;
    [SerializeField]
    private GameObject arrowHolder = null;
    [SerializeField]
    private GameObject iconHolder = null;
    [SerializeField]
    private UnitTypeIcon unitIcon = null;

    [SerializeField]
    private ActionsList actionsList = null;


    private MasterGameTask currentMasterTask;

    private void OnDestroy() {

        if(currentSelection != null) {
            currentSelection.EndSubscriptionToUserActions(this);
            currentSelection.EndSubscriptionToTaskStatus(this);
        }

        if(currentMasterTask != null) {
            currentMasterTask.EndTaskStatusNotifications(this);
        }
    }

    private void UpdateTaskDisplay() {

        infoAreaCanvas.alpha = (currentMasterTask == null) ? 0.65f : 1.0f;

        var displayUnitIcon = currentMasterTask != null && currentMasterTask.assignedUnit != null;
        if(iconHolder.activeSelf != displayUnitIcon) {
            iconHolder.SetActive(displayUnitIcon);
            arrowHolder.SetActive(displayUnitIcon);
        }

        if (displayUnitIcon) {
            unitIcon.SetActionType(currentMasterTask.assignedUnit.primaryActionType);
        }

        taskItemCell.SetTask(currentMasterTask, null);
    }

    /*
     * SelectionManagerDelegate Interface
     * */

    public void NotifyUpdateSelection(Selection nextSelection) {

        if(currentSelection != null) {
            currentSelection.EndSubscriptionToUserActions(this);
            currentSelection.EndSubscriptionToTaskStatus(this);

            if(taskItemCell != null) {
                taskItemCell.SetTask(null, null);
            }
        }

        if(nextSelection != null) {
            titleText.text = nextSelection.Title();

            nextSelection.SubscribeToUserActions(this);
            nextSelection.SubscribeToTaskStatus(this);

        } else {
            titleText.text = noSelectionText;

            actionsList.SetActions(new UserAction[] { });

            taskItemCell.SetTask(null, null);
        }

        currentSelection = nextSelection;
    }

    /*
     * TaskStatusUpdateDelegate Interface
     * */

    public void NowPerformingTask(Unit unit, MasterGameTask masterGameTask, GameTask gameTask) {

        if (currentMasterTask != null) {
            currentMasterTask.EndTaskStatusNotifications(this);
        }

        currentMasterTask = masterGameTask;

        if (currentMasterTask != null) {
            currentMasterTask.RegisterForTaskStatusNotifications(this);
        }

        UpdateTaskDisplay();
    }

    /*
     * UserActionUpdateDelegate Interface
     * */

    public void UpdateUserActionsAvailable(UserAction[] userActions) {
        actionsList.SetActions(userActions);
    }


    /*
     * MasterTaskUpdateDelegate Interface
     * */

    public void TaskBlockerRemoved(MasterGameTask masterGameTask) {
        UpdateTaskDisplay();
    }

    public void TaskUnitAssigned(MasterGameTask masterGameTask) {
        UpdateTaskDisplay();
    }

    public void RepeatCountUpdated(MasterGameTask masterGameTask, int count) { }
    public void TaskCancelled(MasterGameTask masterGameTask) { }    
    public void TaskFinished(MasterGameTask masterGameTask) { }     
}
