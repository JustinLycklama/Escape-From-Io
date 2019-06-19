using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActionableItem : MonoBehaviour, TaskStatusNotifiable {
    string description;

    private MasterGameTask associatedTask;
    //private MasterGameTask[,] associatedTasksCoordinateMap;

    //protected void SetuoCoordinateArrays(int ) {
    //    associatedTasksCoordinateMap = new MasterGameTask[];
    //}

    private List<TaskStatusUpdateDelegate> taskStatusDelegateList = new List<TaskStatusUpdateDelegate>();

    /*
     * Actionable Item Components
     * */ 

    public virtual void AssociateTask(MasterGameTask task) {
        associatedTask = task;

        NotifyAllTaskStatus();
    }

    public virtual void UpdateMasterTaskByGameTask(GameTask gameTask, MasterGameTask masterGameTask) {
        associatedTask = masterGameTask;

        NotifyAllTaskStatus();
    }

    public abstract float performAction(GameTask task, float rate, Unit unit);

    /*
     * TaskStatusUpdateDelegate Interface
     * */

    public virtual void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Add(notificationDelegate);

        // Let the subscriber know our status immediately
        notificationDelegate.NowPerformingTask(associatedTask, null);
    }

    public virtual void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Remove(notificationDelegate);
    }

    protected virtual void NotifyAllTaskStatus() {
        foreach(TaskStatusUpdateDelegate updateDelegate in taskStatusDelegateList) {
            updateDelegate.NowPerformingTask(associatedTask, null);
        }
    }
}
