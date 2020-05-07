using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskAndUnitDetailPanel : NavigationPanel, TaskQueueDelegate, UnitManagerDelegate, TableViewDelegate, GameButtonDelegate {
    private MasterGameTask.ActionType actionType;

    private MasterGameTask[] taskList = new MasterGameTask[0];
    private Unit[] unitList = new Unit[0];

    public Toggle lockTaskListButton;
    public Text TaskListStateLabel;

    public Text taskListTitle;
    public TableView tasksQueueTableView;

    public Text unitListTitle;
    public TableView unitListTableView;

    public UnitTypeIcon unitIconImage;

    protected override void Awake() {
        base.Awake();

        lockTaskListButton.buttonDelegate = this;
    }

    public void SetActionType(MasterGameTask.ActionType actionType) {
        this.actionType = actionType;

        TaskQueueManager taskQueueManager = Script.Get<TaskQueueManager>();
        UnitManager unitManager = Script.Get<UnitManager>();

        unitListTableView.dataDelegate = this;
        unitManager.RegisterForNotifications(this, actionType);

        tasksQueueTableView.dataDelegate = this;
        taskQueueManager.RegisterForNotifications(this, actionType);

        lockTaskListButton.SetState(taskQueueManager.GetTaskListLockStatus(actionType));

        //taskListTitle.text = actionType.TitleAsNoun() + " Backlog";
        unitListTitle.text = actionType.TitleAsNoun() + " Units";

        unitIconImage.SetActionType(actionType);
    }

    private void OnDestroy() {
        try {
            Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
            Script.Get<UnitManager>().EndNotifications(this, actionType);
        } catch(System.NullReferenceException e) { }    
    }

    /*
     * ButtonDelegate Interface
     * */

    public override void ButtonDidClick(GameButton button) {
        base.ButtonDidClick(button);

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
     * UnitManagerDelegate Interface       
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType) {
        this.unitList = unitList;
        unitListTableView.ReloadData();
    }

    /*
     * TableViewDelegate Interface
     * */

    public int NumberOfRows(TableView table) {
        if (table == tasksQueueTableView) {
            return taskList.Length;
        } else if (table == unitListTableView) {
            return unitList.Length;
        }

        return 0;
    }

    public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
        if(table == tasksQueueTableView) {
            TaskDisplayCell taskCell = cell.GetComponent<TaskDisplayCell>();

            taskCell.SetTask(taskList[row]);
        } else if(table == unitListTableView) {
            UnitDisplayCell taskCell = cell.GetComponent<UnitDisplayCell>();

            taskCell.SetUnit(unitList[row]);
        }
    }
}
