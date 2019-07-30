using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TaskAndUnitCell : MonoBehaviour, IPointerClickHandler, TaskQueueDelegate, UnitManagerDelegate, ButtonDelegate {

    public Text title;

    public Text unitCountText;
    public Text unitStatusText;
    public Text unitDurationText; // todo: bar

    public Text taskCountText;
    public Toggle taskListLocked;

    public MasterGameTask.ActionType actionType;

    private void Start() {
        title.text = actionType.ToString();

        taskListLocked.buttonDelegate = this;

        Script.Get<TaskQueueManager>().RegisterForNotifications(this, actionType);
        Script.Get<UnitManager>().RegisterForNotifications(this, actionType);     
    }

    private void OnDestroy() {
        Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
        Script.Get<UnitManager>().EndNotifications(this, actionType);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if (button == taskListLocked) {
            taskListLocked.SetState(!taskListLocked.state);

            Script.Get<TaskQueueManager>().SetTaskListLocked(actionType, taskListLocked.state);
        }
    }

    /*
     * IPointerClickHandler Interface
     * */

    public void OnPointerClick(PointerEventData eventData) {
        TaskAndUnitDetailPanel detailPanel = Script.Get<UIManager>().Push(UIManager.Blueprint.TaskAndUnitDetail) as TaskAndUnitDetailPanel;
        detailPanel.SetActionType(actionType);
    }

    /*
     * TaskQueueDelegate Interface
     * */

    public void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType, TaskQueueManager.ListState listState) {
        taskCountText.text = taskList.Length + " " + actionType.ToString() + " Task" + ((taskList.Length == 1)? "" : "s");

        taskListLocked.SetState(Script.Get<TaskQueueManager>().GetTaskListLockStatus(actionType));
    }

    /*
     * UnitManagerDelegate Interface
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType, Unit.UnitState unitListState) {
        unitCountText.text = unitList.Length + " Unit" + ((unitList.Length == 1) ? "" : "s");
        unitStatusText.text = unitListState.decription();
    }
}
