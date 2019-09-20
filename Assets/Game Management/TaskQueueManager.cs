using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public interface TaskQueueDelegate {
    void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType, TaskQueueManager.ListState listState);
}

public static class ListStateExtensions {
    public static string decription(this TaskQueueManager.ListState listState) {
        switch(listState) {
            case TaskQueueManager.ListState.Empty:
                return "Empty";
            case TaskQueueManager.ListState.Blocked:
                return "Blocked";
            case TaskQueueManager.ListState.Smooth:
                return "Smooth";
            case TaskQueueManager.ListState.Inefficient:
                return "Inefficient";
        }

        return "???";
    }
}

public class TaskQueueManager : MonoBehaviour, UnitManagerDelegate {
    public enum ListState {
        Empty, Blocked, Smooth, Inefficient 
    }

    PlayerBehaviour playerBehaviour;

    //List<MasterGameTask> taskList;
    //UIManager uiManager;

    //List<MasterGameTask> mineTaskList;
    //List<MasterGameTask> moveTaskList;
    //List<MasterGameTask> buildTaskList;

    // Whether if a task list is Locked or not
    Dictionary<MasterGameTask.ActionType, bool> taskListLockMap;

    // The current state of each list
    Dictionary<MasterGameTask.ActionType, ListState> taskListStateMap;


    Dictionary<MasterGameTask.ActionType, List<MasterGameTask>> taskListMap;
    Dictionary<MasterGameTask.ActionType, List<TaskQueueDelegate>> delegateListMap;

    void Awake()
    {
        //mineTaskList = new List<MasterGameTask>();
        //moveTaskList = new List<MasterGameTask>();
        //buildTaskList = new List<MasterGameTask>();

        taskListMap = new Dictionary<MasterGameTask.ActionType, List<MasterGameTask>>();
        delegateListMap = new Dictionary<MasterGameTask.ActionType, List<TaskQueueDelegate>>();

        taskListLockMap = new Dictionary<MasterGameTask.ActionType, bool>();
        taskListStateMap = new Dictionary<MasterGameTask.ActionType, ListState>();

        foreach(MasterGameTask.ActionType actionType in new MasterGameTask.ActionType[] { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move }) {
            taskListMap[actionType] = new List<MasterGameTask>();
            delegateListMap[actionType] = new List<TaskQueueDelegate>();

            taskListLockMap[actionType] = true;
            taskListStateMap[actionType] = ListState.Empty;
        }
    }

    private void Start() {
        playerBehaviour = Script.Get<PlayerBehaviour>();

        StartCoroutine(DishOutTasks());        

        Script.Get<UnitManager>().RegisterForNotifications(this, MasterGameTask.ActionType.Build);
        Script.Get<UnitManager>().RegisterForNotifications(this, MasterGameTask.ActionType.Mine);
        Script.Get<UnitManager>().RegisterForNotifications(this, MasterGameTask.ActionType.Move);
    }

    private void OnDestroy() {
        try {
            Script.Get<UnitManager>().EndNotifications(this, MasterGameTask.ActionType.Build);
            Script.Get<UnitManager>().EndNotifications(this, MasterGameTask.ActionType.Mine);
            Script.Get<UnitManager>().EndNotifications(this, MasterGameTask.ActionType.Move);
        } catch(System.NullReferenceException e) { }
    }

    public void RegisterForNotifications(TaskQueueDelegate notificationDelegate, MasterGameTask.ActionType ofType) {
        delegateListMap[ofType].Add(notificationDelegate);

        notificationDelegate.NotifyUpdateTaskList(taskListMap[ofType].ToArray(), ofType, taskListStateMap[ofType]);
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
            notificationDelegate.NotifyUpdateTaskList(taskListMap[forType].ToArray(), forType, taskListStateMap[forType]);
        }              
    }

    /*
     * Task List Lock Status
     * */

    public void SetTaskListLocked(MasterGameTask.ActionType actionType, bool locked) {
        taskListLockMap[actionType] = locked;

        bool notified = RecalculateState(actionType);
        if (!notified) {
            NotifyDelegates(actionType);
        }
    }

    public bool GetTaskListLockStatus(MasterGameTask.ActionType actionType) {
        return taskListLockMap[actionType];
    }

    /*
     * Task List State
     * */

    // Returns true if notified
    private bool RecalculateState(MasterGameTask.ActionType actionType) {

        UnitManager unitManager = Script.Get<UnitManager>();
        ListState newState = ListState.Empty;

        // TODO: Better state calculation. I want state to change as wrong robots pick up this type of task
        if (taskListMap[actionType].Count > 0) {
            if (taskListLockMap[actionType] == false) {
                newState = ListState.Inefficient;
            } else if (unitManager.GetUnitsOfType(actionType).Length == 0) {
                newState = ListState.Blocked;                
            } else {
                newState = ListState.Smooth;
            }
        }

        if (newState != taskListStateMap[actionType]) {
            taskListStateMap[actionType] = newState;
            NotifyDelegates(actionType);
            return true;
        }

        return false;
    }

    /*
     * UnitManagerDelegate Interface
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType, Unit.UnitState unitListState) {
        RecalculateState(actionType);
    }

    /*
     * Task Handling
     * */


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
            this.refuseTaskList = refuseTaskList ?? new HashSet<int>();
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
    }

    public void RestractTaskRequest(Unit unit) {
        foreach(UnitAndRefused unitAndRefused in unitsRequestingTasks.ToArray()) {
            if (unitAndRefused.unit == unit) {
                unitsRequestingTasks.Remove(unitAndRefused);
                return;
            }
        }        
    }

    // if lastPriorityTasks is true, only look at tasks with alwaysPerformLast as true. Otherwise, ignore these tasks.
    private List<DistanceAndTask> tasksAndDistancesForUnit(UnitAndRefused unitAndRefused, bool lastPriorityTasks) {
        Unit unit = unitAndRefused.unit;
        HashSet<int> refuseTaskList = unitAndRefused.refuseTaskList;

        List<DistanceAndTask> taskDistances = geAvailableTaskDistancesFromList(taskListMap[unit.primaryActionType], unit, lastPriorityTasks, refuseTaskList);

        // There are no tasks available to us from our designated list, check all other open lists
        if(taskDistances.Count == 0) {
            foreach(MasterGameTask.ActionType actionType in new List<MasterGameTask.ActionType>() { MasterGameTask.ActionType.Move, MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine }) {
                if(actionType == unit.primaryActionType || taskListLockMap[actionType] == true) {
                    continue;
                }

                taskDistances = geAvailableTaskDistancesFromList(taskListMap[actionType], unit, lastPriorityTasks, refuseTaskList);

                if(taskDistances.Count > 0) {
                    break;
                }
            }
        }

        return taskDistances;
    }

    IEnumerator DishOutTasks() {
        UnitManager unitManager = Script.Get<UnitManager>();

        HashSet<int> exhaustedTaskNumbers = new HashSet<int>();
        Dictionary<Unit, UnitsDistanceList> unitDistanceListMap = new Dictionary<Unit, UnitsDistanceList>();

        while(true) {

            // Don't perform on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(0.5f);

            //float shortestDistance = float.MaxValue;
            //UnitAndRefused shortestUnitAndRefused = null;

            exhaustedTaskNumbers.Clear();
            unitDistanceListMap.Clear();

            List<DistanceAndTask> takeTaskAttemptList = new List<DistanceAndTask>();

            foreach(UnitAndRefused unitAndRefused in unitsRequestingTasks.ToArray()) {

                Unit localRequestingUnit = unitAndRefused.unit;

                /*
                 * First check if we can take a task from a unit
                 * */

                Unit[] allUnits = unitManager.GetAllUnits();

                Unit[] unitsWithAccessableTasks = allUnits.Where(u => (u.canTakeTaskFromUnit &&

                // Check all units who have tasks of our type, or tasks where the type is unlocked
                u.currentMasterTask.actionType == localRequestingUnit.primaryActionType) || (u.currentMasterTask != null && taskListLockMap[u.currentMasterTask.actionType] == false)).ToArray();

                int waitForRequests = 0;

                // Check each unit to see if we should take their task; we will get to it faster
                foreach(Unit unit in unitsWithAccessableTasks) {
                    Unit localPerformingUnit = unit;

                    if( unitAndRefused.refuseTaskList.Contains(localPerformingUnit.currentMasterTask.taskNumber)) {
                        continue;
                    }

                    waitForRequests++;
                    float unitDistanceLeft = unit.remainingMovementCostOnTask;

                    if (unitDistanceLeft == 0 || float.IsNaN(unitDistanceLeft)) {
                        unitDistanceLeft = 0.0001f;
                    }

                    PathRequestManager.RequestPathForTask(localRequestingUnit.transform.position, localRequestingUnit.movementPenaltyMultiplier, localPerformingUnit.takeableTask, (LookPoint[] lookPoints, ActionableItem item, bool success, int distance) => {
                        float distanceForUnit = distance * localRequestingUnit.speed;

                        print("Distance For Searching Unit " + distanceForUnit);
                        print("Distance For Performing Unit " + unitDistanceLeft);

                        if (distanceForUnit / unitDistanceLeft < 0.75f) {
                            takeTaskAttemptList.Add(new DistanceAndTask(localPerformingUnit.currentMasterTask, distanceForUnit, unitAndRefused, localPerformingUnit));
                        } else {
                            unitAndRefused.refuseTaskList.Add(localPerformingUnit.currentMasterTask.taskNumber);
                        }

                        waitForRequests--;
                    });
                }

                yield return new WaitUntil(() => {
                    return waitForRequests == 0;
                });

                /*
                 * Look for tasks within our queue
                 * */

                List <DistanceAndTask> taskDistances = tasksAndDistancesForUnit(unitAndRefused, false);

                // If we can't find any tasks to do, check the low priority tasks from all Action Types
                if (taskDistances.Count == 0) {
                    taskDistances = tasksAndDistancesForUnit(unitAndRefused, true);
                }

                // Don't even bother if this unit can't do anything
                if (taskDistances.Count > 0) {
                    unitDistanceListMap[unitAndRefused.unit] = new UnitsDistanceList(unitAndRefused, taskDistances, taskDistances[0].distance);
                }               
            }

            /*
             * Give out tasks that should be taken from other units
             * */

            List<DistanceAndTask> sortedDistances = takeTaskAttemptList.OrderBy(dt => dt.distance).ToList();
            HashSet<Unit> unitsWhoHaveHadTaskTaken = new HashSet<Unit>();

            for(int i = 0; i < sortedDistances.Count; i++) {
                DistanceAndTask distanceAndTask = sortedDistances[i];

                // Unit has already had their task taken, cannot happen twice
                if(unitsWhoHaveHadTaskTaken.Contains(distanceAndTask.performingUnit)) {
                    continue;
                }

                distanceAndTask.performingUnit.CancelTask();
                distanceAndTask.requestingUnit.callback(distanceAndTask.masterTask);

                unitsWhoHaveHadTaskTaken.Add(distanceAndTask.performingUnit);

                unitDistanceListMap.Remove(distanceAndTask.requestingUnit.unit);
                unitsRequestingTasks.Remove(distanceAndTask.requestingUnit);
            }

            /*
             * Give out tasks within our queue, for units that were not already assigned during task taking
             * */

            // Give the unit with the closest viable task its task, remove that task from any others down the list, and do it all again
            while (unitDistanceListMap.Count > 0) {
                List<UnitsDistanceList> unitDistanceListList = unitDistanceListMap.Values.ToList();

                unitDistanceListList.ToList().Sort(delegate (UnitsDistanceList t1, UnitsDistanceList t2) {
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
                        taskListMap[masterTask.actionType].Remove(masterTask);
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
                            unitDistanceListMap.Remove(otherUnitDistanceList.unitAndRefused.unit);
                        }
                    }
                }

                // Give the unit this task, then remove him from the list of people with viable tasks, AND from the list of requesters
                unitsDistanceList.unitAndRefused.callback(givingTask);

                unitDistanceListMap.Remove(unitsDistanceList.unitAndRefused.unit);
                unitsRequestingTasks.Remove(unitsDistanceList.unitAndRefused);                

                NotifyDelegates(givingTask.actionType);
            }
        }
    }

    struct DistanceAndTask {
        public MasterGameTask masterTask;
        public float distance;

        public UnitAndRefused requestingUnit;
        public Unit performingUnit;

        public DistanceAndTask(MasterGameTask masterTask, float distance, UnitAndRefused requestingUnit = null, Unit performingUnit = null) : this() {
            this.masterTask = masterTask;
            this.distance = distance;

            this.requestingUnit = requestingUnit;
            this.performingUnit = performingUnit;
        }        
    }

    private List<DistanceAndTask> geAvailableTaskDistancesFromList(List<MasterGameTask> taskList, Unit unit, bool lastPriorityTasks, HashSet<int> refuseTaskList = null) {
        //float shortestDistance = float.MaxValue;
        //MasterGameTask shortestTask = null;

        List<DistanceAndTask> distanceAndTasks = new List<DistanceAndTask>();

        foreach(MasterGameTask masterTask in taskList) {

            // Skip any tasks that are not in our priority filter
            if (masterTask.alwaysPerformLast != lastPriorityTasks) {
                continue;
            }

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
