using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskQueueManager : MonoBehaviour
{
    //List<MasterGameTask> taskList;
    UIManager uiManager;

    List<MasterGameTask> mineTaskList;
    List<MasterGameTask> moveTaskList;
    List<MasterGameTask> buildTaskList;

    void Awake()
    {
        mineTaskList = new List<MasterGameTask>();
        moveTaskList = new List<MasterGameTask>();
        buildTaskList = new List<MasterGameTask>();
    }

    private void Start() {
        uiManager = Script.Get<UIManager>();
    }

    public void QueueTask(MasterGameTask task) {
        switch(task.actionType) {
            case MasterGameTask.ActionType.Mine:
                mineTaskList.Add(task);
                break;
            case MasterGameTask.ActionType.Build:
                buildTaskList.Add(task);
                break;
            case MasterGameTask.ActionType.Move:
                moveTaskList.Add(task);
                break;
        }

        //uiManager.UpdateTaskList(mineTaskList.InsertRange(moveTaskList.Count, moveTaskList).toArray());
    }

    public MasterGameTask GetNextDoableTask(Unit unit) {
        switch(unit.primaryActionType) {
            case MasterGameTask.ActionType.Mine:
                return getNextAvailableFromList(mineTaskList, unit);
            case MasterGameTask.ActionType.Build:
                return getNextAvailableFromList(buildTaskList, unit);
            case MasterGameTask.ActionType.Move:
                return getNextAvailableFromList(moveTaskList, unit);
        }

        return null;
    }

    private MasterGameTask getNextAvailableFromList(List<MasterGameTask> taskList, Unit unit) {
        float shortestDistance = float.MaxValue;
        MasterGameTask shortestTask = null;

        foreach(MasterGameTask masterTask in taskList) {
            if(masterTask.SatisfiesStartRequirements()) {

                if(masterTask.childTasks[0].pathRequestTargetType != PathRequestTargetType.Unknown) {
                    float distance = Vector3.Distance(masterTask.childTasks[0].target.vector3, unit.transform.position);

                    if(distance < shortestDistance) {
                        shortestDistance = distance;
                        shortestTask = masterTask;
                    }
                } else {
                    if(shortestTask == null) {
                        shortestTask = masterTask;
                    }

                    // If we have an unknown task, this is the end of our search. 
                    // Either we have a task above that is more important, and the shortest of those is the victor, or we use this unknown as the shortest task;
                    break;
                }
            }
        }

        if(shortestTask != null) {
            taskList.Remove(shortestTask);
            uiManager.UpdateTaskList(taskList.ToArray());
        }

        return shortestTask;
    }
}
