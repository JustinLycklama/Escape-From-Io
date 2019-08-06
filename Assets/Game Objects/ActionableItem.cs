using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActionableItem : MonoBehaviour, TaskStatusNotifiable {
    public string description;

    // taskAlreadyDictated is used when a task is about to be assoctaed with this object, 
    // but has not quite been associated, to let everybody know that this is taken

    [HideInInspector]
    public bool taskAlreadyDictated = false; 
    public MasterGameTask associatedTask { get; private set; }

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
        taskAlreadyDictated = false;

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
        notificationDelegate.NowPerformingTask(null, associatedTask, null);
    }

    public virtual void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Remove(notificationDelegate);
    }

    protected virtual void NotifyAllTaskStatus() {
        foreach(TaskStatusUpdateDelegate updateDelegate in taskStatusDelegateList) {
            updateDelegate.NowPerformingTask(null, associatedTask, null);
        }
    }
}
