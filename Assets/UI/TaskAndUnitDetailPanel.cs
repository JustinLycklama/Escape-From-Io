using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskAndUnitDetailPanel : NavigationPanel, TaskQueueDelegate, TableViewDelegate {
    private MasterGameTask.ActionType actionType;

    private MasterGameTask[] taskList = new MasterGameTask[0];
    public TableView tasksQueueTableView;


    public void SetActionType(MasterGameTask.ActionType actionType) {
        this.actionType = actionType;

        tasksQueueTableView.dataDelegate = this;
        Script.Get<TaskQueueManager>().RegisterForNotifications(this, actionType);
    }

    private void OnDestroy() {
        Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
    }

    /*
     * TaskQueueDelegate Interface
     * */

    public void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType) {
        this.taskList = taskList;
        tasksQueueTableView.ReloadData();
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
