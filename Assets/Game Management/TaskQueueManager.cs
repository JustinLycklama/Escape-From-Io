using System;
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

    private void Start() {
        StartCoroutine(DishOutTasks());
    }

    public void RegisterForNotifications(TaskQueueDelegate notificationDelegate, MasterGameTask.ActionType ofType) {
        delegateListMap[ofType].Add(notificationDelegate);

        notificationDelegate.NotifyUpdateTaskList(taskListMap[ofType].ToArray(), ofType);
    }

    public void EndNotifications(TaskQueueDelegate notificationDelegate, MasterGameTask.ActionType forType) {
        delegateListMap[forType].Remove(notificationDelegate);
    }

    private void NotifyDelegates(HashSet<MasterGameTask.ActionType> forTypes) {
        foreach(MasterGameTask.ActionType forType in forTypes) {
            NotifyDelegates(forType);
        }
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

    class UnitAndRefused {
        public Unit unit;
        public HashSet<int> refuseTaskList;
        public Action<MasterGameTask> callback;

        public UnitAndRefused(Unit unit, HashSet<int> refuseTaskList, Action<MasterGameTask> callback) {
            this.unit = unit;
            this.refuseTaskList = refuseTaskList;
            this.callback = callback;
        }
    }

    class UnitsDistanceList {
        public UnitAndRefused unitAndRefused;

        public List<DistanceAndTask> distanceAndTasksList;
        public float shortestTaskDistance;

        public UnitsDistanceList(UnitAndRefused unitAndRefused, List<DistanceAndTask> distanceAndTasksList, float shortestTaskDistance) {
            this.unitAndRefused = unitAndRefused;
            this.distanceAndTasksList = distanceAndTasksList;
            this.shortestTaskDistance = shortestTaskDistance;
        }
    }

    List<UnitAndRefused> unitsRequestingTasks = new List<UnitAndRefused>();

    public void RequestNextDoableTask(Unit unit, Action<MasterGameTask> callback, HashSet<int> refuseTaskList = null) {


        unitsRequestingTasks.Add(new UnitAndRefused(unit, refuseTaskList, callback));

        //return getNextAvailableFromList(taskListMap[unit.primaryActionType], unit, refuseTaskList);

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

    IEnumerator DishOutTasks() {
        HashSet<int> exhaustedTaskNumbers = new HashSet<int>();
        List<UnitsDistanceList> unitDistanceListList = new List<UnitsDistanceList>();

        while(true) {
            yield return new WaitForSeconds(0.5f);

            //float shortestDistance = float.MaxValue;
            //UnitAndRefused shortestUnitAndRefused = null;

            exhaustedTaskNumbers.Clear();
            unitDistanceListList.Clear();

            foreach(UnitAndRefused unitAndRefused in unitsRequestingTasks) {
                Unit unit = unitAndRefused.unit;
                HashSet<int> refuseTaskList = unitAndRefused.refuseTaskList;

                List<DistanceAndTask> taskDistances = geAvailableTaskDistancesFromList(taskListMap[unit.primaryActionType], unit, refuseTaskList);
                
                // Don't even bother if this unit can't do anything
                if (taskDistances.Count > 0) {
                    unitDistanceListList.Add(new UnitsDistanceList(unitAndRefused, taskDistances, taskDistances[0].distance));
                }               
            }

            // Give the unit with the closest viable task its task, remove that task from any others down the list, and do it all again
            while (unitDistanceListList.Count > 0) {

                unitDistanceListList.Sort(delegate (UnitsDistanceList t1, UnitsDistanceList t2) {
                    return t1.shortestTaskDistance.CompareTo(t2.shortestTaskDistance);
                });

                UnitsDistanceList unitsDistanceList = unitDistanceListList[0];

                // Lets assume that the first item of a units list is always available
                MasterGameTask givingTask = unitsDistanceList.distanceAndTasksList[0].masterTask;

                bool doExhaust = false;
                if(givingTask.repeatCount == 0) {
                    doExhaust = true;
                    exhaustedTaskNumbers.Add(givingTask.taskNumber);
                    taskListMap[givingTask.actionType].Remove(givingTask);
                } else {
                    MasterGameTask masterTask = givingTask;
                    givingTask = givingTask.CloneTask();

                    if(masterTask.repeatCount == 0) {
                        // Exhaust
                        doExhaust = true;
                        exhaustedTaskNumbers.Add(masterTask.taskNumber);
                        taskListMap[givingTask.actionType].Remove(givingTask);
                    }
                }

                if (doExhaust) {
                    // Exhaust tasks from the top of all other lists until a nonexhausted task is found
                    UnitsDistanceList[] unitDistanceListListCopy = unitDistanceListList.ToArray();
                    for(int i = 1; i < unitDistanceListListCopy.Length; i++) {
                        UnitsDistanceList otherUnitDistanceList = unitDistanceListListCopy[i];

                        while(otherUnitDistanceList.distanceAndTasksList.Count > 0 && exhaustedTaskNumbers.Contains(otherUnitDistanceList.distanceAndTasksList[0].masterTask.taskNumber)) {
                            // If the other list contains an exhausted task number, remove it and continue
                            otherUnitDistanceList.distanceAndTasksList.RemoveAt(0);
                        }

                        // If we have removed all viable tasks from this units list, he cannot be completed
                        if(otherUnitDistanceList.distanceAndTasksList.Count == 0) {
                            unitDistanceListList.Remove(otherUnitDistanceList);
                        }
                    }
                }

                // Give the unit this task, then remove him from the list of people with viable tasks, AND from the list of requesters
                unitsDistanceList.unitAndRefused.callback(givingTask);

                unitDistanceListList.Remove(unitsDistanceList);
                unitsRequestingTasks.Remove(unitsDistanceList.unitAndRefused);

                NotifyDelegates(givingTask.actionType);
            }



            // TODO: NOTIFY
            //NotifyDelegates(new HashSet<MasterGameTask.ActionType>() { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move });



            //UnitAndRefused receivingUnit = unitsDistanceList.unit;
            //List<DistanceAndTask> taskDistances = unitsDistanceList.distanceAndTasksList;

            // Find a task that has not already been taken
            //MasterGameTask viableTask = null;
            //for (int i = 0; i < unitsDistanceList.distanceAndTasksList.Count; i++) {
            //    MasterGameTask currentTask = taskDistances[i].masterTask;
            //    if (exhaustedTaskNumbers.Contains(currentTask.taskNumber))

            //}


            






        }
    }

    struct DistanceAndTask {
        public MasterGameTask masterTask;
        public float distance;

        public DistanceAndTask(MasterGameTask masterTask, float distance) : this() {
            this.masterTask = masterTask;
            this.distance = distance;
        }
    }

    private List<DistanceAndTask> geAvailableTaskDistancesFromList(List<MasterGameTask> taskList, Unit unit, HashSet<int> refuseTaskList = null) {
        //float shortestDistance = float.MaxValue;
        //MasterGameTask shortestTask = null;

        List<DistanceAndTask> distanceAndTasks = new List<DistanceAndTask>();

        foreach(MasterGameTask masterTask in taskList) {

            if (refuseTaskList != null && refuseTaskList.Contains(masterTask.taskNumber)) {
                continue;
            }

            if(masterTask.SatisfiesStartRequirements()) {

                if(masterTask.childGameTasks[0].pathRequestTargetType != PathRequestTargetType.Unknown) {
                    float distance = Vector3.Distance(masterTask.childGameTasks[0].target.vector3, unit.transform.position);

                    distanceAndTasks.Add(new DistanceAndTask(masterTask, distance));

                    //if(distance < shortestDistance) {
                    //    shortestDistance = distance;
                    //    shortestTask = masterTask;
                    //}
                } else {
                    distanceAndTasks.Add(new DistanceAndTask(masterTask, float.MaxValue));

                    //if(shortestTask == null) {
                    //    shortestTask = masterTask;
                    //}

                    // If we have an unknown task, this is the end of our search. 
                    // Either we have a task above that is more important, and the shortest of those is the victor, or we use this unknown as the shortest task;
                    break;
                }
            }
        }

        distanceAndTasks.Sort(delegate (DistanceAndTask t1, DistanceAndTask t2) {
            return t1.distance.CompareTo(t2.distance);
        });

        return distanceAndTasks;

        //if(shortestTask != null) {

        //    if (shortestTask.repeatCount == 0) {
        //        taskList.Remove(shortestTask);
        //    } else {
        //        MasterGameTask masterTask = shortestTask;
        //        shortestTask = shortestTask.CloneTask();

        //        if (masterTask.repeatCount == 0) {
        //            taskList.Remove(masterTask);
        //        }
        //    }

        //    NotifyDelegates(shortestTask.actionType);
        //    //uiManager.UpdateTaskList(taskList.ToArray());
        //}

        //return shortestTask;
    }
}
