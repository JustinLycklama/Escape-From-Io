using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TaskQueueDelegate {
    void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType);
}

public class TaskQueueManager : MonoBehaviour
{
    //List<MasterGameTask> taskList;
    //UIManager uiManager;

    //List<MasterGameTask> mineTaskList;
    //List<MasterGameTask> moveTaskList;
    //List<MasterGameTask> buildTaskList;

    Dictionary<MasterGameTask.ActionType, List<MasterGameTask>> taskListMap;
    Dictionary<MasterGameTask.ActionType, List<TaskQueueDelegate>> delegateListMap;

    void Awake()
    {
        //mineTaskList = new List<MasterGameTask>();
        //moveTaskList = new List<MasterGameTask>();
        //buildTaskList = new List<MasterGameTask>();

        taskListMap = new Dictionary<MasterGameTask.ActionType, List<MasterGameTask>>();
        delegateListMap = new Dictionary<MasterGameTask.ActionType, List<TaskQueueDelegate>>();

        foreach(MasterGameTask.ActionType actionType in new MasterGameTask.ActionType[] { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move }) {
            taskListMap[actionType] = new List<MasterGameTask>();
            delegateListMap[actionType] = new List<TaskQueueDelegate>();
        }
    }

    //private void Start() {
    //    //uiManager = Script.Get<UIManager>();
    //}

    public void RegisterForNotifications(TaskQueueDelegate notificationDelegate, MasterGameTask.ActionType ofType) {
        delegateListMap[ofType].Add(notificationDelegate);

        notificationDelegate.NotifyUpdateTaskList(taskListMap[ofType].ToArray(), ofType);
    }

    public void EndNotifications(TaskQueueDelegate notificationDelegate, MasterGameTask.ActionType forType) {
        delegateListMap[forType].Remove(notificationDelegate);
    }

    private void NotifyDelegates(MasterGameTask.ActionType forType) {
        foreach(TaskQueueDelegate notificationDelegate in delegateListMap[forType]) {
            notificationDelegate.NotifyUpdateTaskList(taskListMap[forType].ToArray(), forType);
        }
    }

    // If a unit cannot handle a task, he can put it back
    public void PutBackTask(MasterGameTask task) {
        task.assignedUnit = null;

        taskListMap[task.actionType].Insert(0, task);
        NotifyDelegates(task.actionType);
    }

    public void QueueTask(MasterGameTask task) {
        task.assignedUnit = null;

        taskListMap[task.actionType].Add(task);
        NotifyDelegates(task.actionType);

        //switch(task.actionType) {
        //    case MasterGameTask.ActionType.Mine:
        //        mineTaskList.Add(task);
        //        break;
        //    case MasterGameTask.ActionType.Build:
        //        buildTaskList.Add(task);
        //        break;
        //    case MasterGameTask.ActionType.Move:
        //        moveTaskList.Add(task);
        //        break;
        //}

        //uiManager.UpdateTaskList(mineTaskList.InsertRange(moveTaskList.Count, moveTaskList).toArray());
    }

    public void DeQueueTask(MasterGameTask task) {
        task.assignedUnit = null;

        taskListMap[task.actionType].Remove(task);
        NotifyDelegates(task.actionType);
    }

    public MasterGameTask GetNextDoableTask(Unit unit, HashSet<int> refuseTaskList = null) {
        return getNextAvailableFromList(taskListMap[unit.primaryActionType], unit, refuseTaskList);

        //switch(unit.primaryActionType) {
        //    case MasterGameTask.ActionType.Mine:
        //        return getNextAvailableFromList(mineTaskList, unit);
        //    case MasterGameTask.ActionType.Build:
        //        return getNextAvailableFromList(buildTaskList, unit);
        //    case MasterGameTask.ActionType.Move:
        //        return getNextAvailableFromList(moveTaskList, unit);
        //}

        //return null;
    }

    private MasterGameTask getNextAvailableFromList(List<MasterGameTask> taskList, Unit unit, HashSet<int> refuseTaskList = null) {
        float shortestDistance = float.MaxValue;
        MasterGameTask shortestTask = null;

        foreach(MasterGameTask masterTask in taskList) {

            if (refuseTaskList != null && refuseTaskList.Contains(masterTask.taskNumber)) {
                continue;
            }

            if(masterTask.SatisfiesStartRequirements()) {

                if(masterTask.childGameTasks[0].pathRequestTargetType != PathRequestTargetType.Unknown) {
                    float distance = Vector3.Distance(masterTask.childGameTasks[0].target.vector3, unit.transform.position);

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

            if (shortestTask.repeatCount == 0) {
                taskList.Remove(shortestTask);
            } else {
                MasterGameTask masterTask = shortestTask;
                shortestTask = shortestTask.CloneTask();

                if (masterTask.repeatCount == 0) {
                    taskList.Remove(masterTask);
                }
            }

            NotifyDelegates(shortestTask.actionType);
            //uiManager.UpdateTaskList(taskList.ToArray());
        }

        return shortestTask;
    }
}
