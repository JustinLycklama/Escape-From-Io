using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentSelectionPanel : NavigationPanel, SelectionManagerDelegate, TaskStatusUpdateDelegate, UserActionUpdateDelegate {

    Selection currentSelection;

    const string noSelectionText = "None";

    [SerializeField]
    private Text title;

    [SerializeField]
    private MasterAndGameTaskCell taskItemCell;

    [SerializeField]
    private ActionsList actionsList;

    void Start() {
        base.Start();

        title.text = noSelectionText;
        Script.Get<SelectionManager>().RegisterForNotifications(this);

        //NotifyUpdateSelection(null);
    }

    private void OnDestroy() {

        if(currentSelection != null) {
            currentSelection.EndSubscriptionToUserActions(this);
            currentSelection.EndSubscriptionToTaskStatus(this);
        }

        Script.Get<SelectionManager>().EndNotifications(this);
    }

    /*
     * SelectionManagerDelegate Interface
     * */

    public void NotifyUpdateSelection(Selection nextSelection) {

        if(currentSelection != null) {
            currentSelection.EndSubscriptionToUserActions(this);
            currentSelection.EndSubscriptionToTaskStatus(this);

            if(currentGameAndTaskCell != null) {
                currentGameAndTaskCell.SetTask(null, null);
            }
        }

        if(nextSelection != null) {
            title.text = nextSelection.Title();

            nextSelection.SubscribeToUserActions(this);
            nextSelection.SubscribeToTaskStatus(this);

        } else {
            title.text = noSelectionText;
            actionsList.SetActions(new UserAction[] { });

            currentGameAndTaskCell = null;
        }

        currentSelection = nextSelection;
    }

    /*
     * TaskStatusUpdateDelegate Interface
     * */
    private MasterAndGameTaskCell currentGameAndTaskCell;

    public void NowPerformingTask(Unit unit, MasterGameTask masterGameTask, GameTask gameTask) {
        if(currentGameAndTaskCell != null) {
            currentGameAndTaskCell.SetTask(masterGameTask, gameTask);
        }
    }

    /*
     * UserActionUpdateDelegate Interface
     * */

    public void UpdateUserActionsAvailable(UserAction[] userActions) {
        actionsList.SetActions(userActions);
    }
}
