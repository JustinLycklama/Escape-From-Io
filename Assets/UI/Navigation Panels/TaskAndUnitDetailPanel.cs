using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskAndUnitDetailPanel : NavigationPanel, TaskQueueDelegate, TableViewDelegate, ButtonDelegate {
    private MasterGameTask.ActionType actionType;

    private MasterGameTask[] taskList = new MasterGameTask[0];

    public Toggle lockTaskListButton;
    public Text TaskListStateLabel;

    public Text taskListTitle;
    public TableView tasksQueueTableView;

    public Text unitListTitle;
    public TableView unitListTableView;

    protected override void Awake() {
        base.Awake();

        lockTaskListButton.buttonDelegate = this;
    }

    public void SetActionType(MasterGameTask.ActionType actionType) {
        this.actionType = actionType;

        TaskQueueManager taskQueueManager = Script.Get<TaskQueueManager>();

        tasksQueueTableView.dataDelegate = this;
        taskQueueManager.RegisterForNotifications(this, actionType);
        lockTaskListButton.SetState(taskQueueManager.GetTaskListLockStatus(actionType));
    }

    private void OnDestroy() {
        Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {    
        if (button == lockTaskListButton) {
            lockTaskListButton.SetState(!lockTaskListButton.state);

            Script.Get<TaskQueueManager>().SetTaskListLocked(actionType, lockTaskListButton.state);
        }
    }

    /*
     * TaskQueueDelegate Interface
     * */

    public void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType, TaskQueueManager.ListState listState) {
        this.taskList = taskList;
        tasksQueueTableView.ReloadData();

        TaskListStateLabel.text = listState.decription();
    }

    /*
     * TableViewDelegate Interface
     * */

    public int NumberOfRows(TableView table) {
        return taskList.Length;
    }

    public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
        TaskDisplayCell taskCell = cell.GetComponent<TaskDisplayCell>();

        taskCell.SetTask(taskList[row]);
    }
}
