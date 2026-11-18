using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Registers for individual action types
public interface TaskQueueDelegate {
    void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType, TaskQueueManager.ListState listState);
}

// Registers for all lock status
public interface TaskLockUpdateDelegate {
    void NotifyUpdateLockState(MasterGameTask.ActionType actionType, bool locked);
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

    // Hack for restarting queue manager on crash
    public DateTime lastDished = DateTime.Now;

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

    List<TaskLockUpdateDelegate> lockDelegateList;

    void Awake()
    {
        //mineTaskList = new List<MasterGameTask>();
        //moveTaskList = new List<MasterGameTask>();
        //buildTaskList = new List<MasterGameTask>();

        taskListMap = new Dictionary<MasterGameTask.ActionType, List<MasterGameTask>>();
        delegateListMap = new Dictionary<MasterGameTask.ActionType, List<TaskQueueDelegate>>();

        lockDelegateList = new List<TaskLockUpdateDelegate>();

        taskListLockMap = new Dictionary<MasterGameTask.ActionType, bool>();
        taskListStateMap = new Dictionary<MasterGameTask.ActionType, ListState>();

        foreach(MasterGameTask.ActionType actionType in new MasterGameTask.ActionType[] { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move, MasterGameTask.ActionType.Attack }) {
            taskListMap[actionType] = new List<MasterGameTask>();
            delegateListMap[actionType] = new List<TaskQueueDelegate>();

            taskListLockMap[actionType] = true;
            taskListStateMap[actionType] = ListState.Empty;
        }
    }

    Coroutine assignTasksRoutine;

    private void Start() {
        playerBehaviour = Script.Get<PlayerBehaviour>();

        assignTasksRoutine = StartCoroutine(DishOutTasks());        

        Script.Get<UnitManager>().RegisterForNotifications(this, MasterGameTask.ActionType.Build);
        Script.Get<UnitManager>().RegisterForNotifications(this, MasterGameTask.ActionType.Mine);
        Script.Get<UnitManager>().RegisterForNotifications(this, MasterGameTask.ActionType.Move);
        Script.Get<UnitManager>().RegisterForNotifications(this, MasterGameTask.ActionType.Attack);
    }

    public void RestartDishOut() {
        StopCoroutine(assignTasksRoutine);
        assignTasksRoutine = StartCoroutine(DishOutTasks());
    }

    private void OnDestroy() {
        try {
            Script.Get<UnitManager>().EndNotifications(this, MasterGameTask.ActionType.Build);
            Script.Get<UnitManager>().EndNotifications(this, MasterGameTask.ActionType.Mine);
            Script.Get<UnitManager>().EndNotifications(this, MasterGameTask.ActionType.Move);
            Script.Get<UnitManager>().EndNotifications(this, MasterGameTask.ActionType.Attack);
        } catch(NullReferenceException) { }
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

        TutorialManager.sharedInstance.Fire(TutorialTrigger.TaskLockToggle);
        NotifyLockUpdates(actionType);
    }

    public bool GetTaskListLockStatus(MasterGameTask.ActionType actionType) {
        return taskListLockMap[actionType];
    }

    public void RegisterForLockStatusUpdates(TaskLockUpdateDelegate notificationDelegate) {
        lockDelegateList.Add(notificationDelegate);
    }

    public void EndLockUpdates(TaskLockUpdateDelegate notificationDelegate) {
        lockDelegateList.Remove(notificationDelegate);
    }

    private void NotifyLockUpdates(MasterGameTask.ActionType actionType) {
        foreach(TaskLockUpdateDelegate notificationDelegate in lockDelegateList) {
            notificationDelegate.NotifyUpdateLockState(actionType, taskListLockMap[actionType]);
        }
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
            } else if (unitManager.GetPlayerUnitsOfType(actionType).Length == 0) {
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

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType) {
        RecalculateState(actionType);
    }

    /*
     * Task Handling
     * */


    // If a unit cannot handle a task, he can put it back
    public void PutBackTask(MasterGameTask task) {
        task.SetAssignedUnit(null);

        taskListMap[task.actionType].Insert(0, task);
        NotifyDelegates(task.actionType);
    }

    public void QueueTask(MasterGameTask task) {
        task.SetAssignedUnit(null);

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
        task.SetAssignedUnit(null);

        taskListMap[task.actionType].Remove(task);
        NotifyDelegates(task.actionType);
    }

    class UnitAndRefused {
        public Unit unit;
        public HashSet<int> refuseTaskList;
        public HashSet<int> refuseStealTaskList;

        public Action<MasterGameTask> callback;

        public UnitAndRefused(Unit unit, HashSet<int> refuseTaskList, Action<MasterGameTask> callback) {
            this.unit = unit;
            this.refuseTaskList = refuseTaskList ?? new HashSet<int>();
            refuseStealTaskList = new HashSet<int>();
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

    // One-line state dump for watchdog logs and debugging
    public string DiagnosticSummary() {
        string lists = string.Join(", ", taskListMap.Select(kv => $"{kv.Key}={kv.Value.Count}{(taskListLockMap[kv.Key] ? "" : "(unlocked)")}"));
        return $"[TaskQueue lists: {lists}; units waiting: {unitsRequestingTasks.Count}]";
    }

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

    // Longest the steal step waits for its path requests before moving on (searches normally finish synchronously)
    private const float maxStealPathWaitSeconds = 2f;

    IEnumerator DishOutTasks() {
        UnitManager unitManager = Script.Get<UnitManager>();

        HashSet<int> exhaustedTaskNumbers = new HashSet<int>();
        Dictionary<Unit, UnitsDistanceList> unitDistanceListMap = new Dictionary<Unit, UnitsDistanceList>();

        while(true) {

            lastDished = DateTime.Now;

            // Don't perform on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(0.5f);

            exhaustedTaskNumbers.Clear();
            unitDistanceListMap.Clear();

            // Units destroyed while waiting (Unity null) can't take work
            unitsRequestingTasks.RemoveAll(r => r.unit == null);

            List<DistanceAndTask> takeTaskAttemptList = new List<DistanceAndTask>();

            foreach(UnitAndRefused unitAndRefused in unitsRequestingTasks.ToArray()) {

                /*
                 * First check if we can take a task from a unit
                 * */

                int waitForRequests = 0;

                try {
                    waitForRequests = RequestStealCandidates(unitAndRefused, unitManager, takeTaskAttemptList, () => waitForRequests--);
                } catch(Exception ex) {
                    Debug.LogException(ex);
                }

                float waitStarted = Time.realtimeSinceStartup;
                yield return new WaitUntil(() => {
                    return waitForRequests <= 0 || Time.realtimeSinceStartup - waitStarted > maxStealPathWaitSeconds;
                });

                lastDished = DateTime.Now;

                /*
                 * Look for tasks within our queue
                 * */

                try {
                    List<DistanceAndTask> taskDistances = tasksAndDistancesForUnit(unitAndRefused, false);

                    // If we can't find any tasks to do, check the low priority tasks from all Action Types
                    if(taskDistances.Count == 0) {
                        taskDistances = tasksAndDistancesForUnit(unitAndRefused, true);
                    }

                    // Don't even bother if this unit can't do anything
                    if(taskDistances.Count > 0 && unitAndRefused.unit != null) {
                        unitDistanceListMap[unitAndRefused.unit] = new UnitsDistanceList(unitAndRefused, taskDistances, taskDistances[0].distance);
                    }
                } catch(Exception ex) {
                    Debug.LogException(ex);
                }
            }

            try {
                GiveOutStolenTasks(takeTaskAttemptList, unitDistanceListMap);
                GiveOutQueuedTasks(unitDistanceListMap, exhaustedTaskNumbers);
            } catch(Exception ex) {
                // Never let one bad hand-out kill the assignment loop
                Debug.LogException(ex);
            }
        }
    }

    // Requests paths to tasks other units are doing, to see if this unit would reach them much sooner.
    // Returns how many requests were made; onRequestDone runs once per request (from its callback, or immediately if no request was made).
    private int RequestStealCandidates(UnitAndRefused unitAndRefused, UnitManager unitManager, List<DistanceAndTask> takeTaskAttemptList, Action onRequestDone) {
        Unit localRequestingUnit = unitAndRefused.unit;
        if(localRequestingUnit == null) { return 0; }

        Unit[] allUnits = unitManager.GetAllPlayerUnits();

        // Units whose task is of our type, or whose task list is unlocked (canTakeTaskFromUnit implies currentMasterTask != null)
        Unit[] unitsWithAccessableTasks = allUnits.Where(u => u != null && u.canTakeTaskFromUnit &&
            (u.currentMasterTask.actionType == localRequestingUnit.primaryActionType || taskListLockMap[u.currentMasterTask.actionType] == false)).ToArray();

        int pending = 0;

        // Check each unit to see if we should take their task; we will get to it faster
        foreach(Unit unit in unitsWithAccessableTasks) {
            Unit localPerformingUnit = unit;
            MasterGameTask stealTarget = unit.currentMasterTask;
            float unitDistanceLeft = unit.remainingMovementCostOnTask;

            if(unitDistanceLeft < 100 || float.IsNaN(unitDistanceLeft)) {
                continue;
            }

            if(unitAndRefused.refuseStealTaskList.Contains(stealTarget.taskNumber)) {
                continue;
            }

            pending++;
            bool done = false;

            PathRequest request = PathRequestManager.RequestPathForTask(localRequestingUnit.transform.position, localRequestingUnit.movementPenaltyMultiplier, localPerformingUnit.takeableTask, (LookPoint[] lookPoints, ActionableItem item, bool success, int distance) => {
                done = true;
                onRequestDone();

                // Either unit may be gone, or the task finished, by the time the path comes back
                if(localRequestingUnit == null || localPerformingUnit == null || localPerformingUnit.currentMasterTask != stealTarget) {
                    return;
                }

                float distanceForUnit = distance * localRequestingUnit.unitSpeed;

                if(success && distanceForUnit / unitDistanceLeft < 0.75f) {
                    takeTaskAttemptList.Add(new DistanceAndTask(stealTarget, distanceForUnit, unitAndRefused, localPerformingUnit));
                } else {
                    unitAndRefused.refuseStealTaskList.Add(stealTarget.taskNumber);
                }
            });

            // No request was made (unsupported target), so no callback will come
            if(request == null && !done) {
                onRequestDone();
            }
        }

        return pending;
    }

    // Give out tasks that should be taken from other units, closest first
    private void GiveOutStolenTasks(List<DistanceAndTask> takeTaskAttemptList, Dictionary<Unit, UnitsDistanceList> unitDistanceListMap) {
        List<DistanceAndTask> sortedDistances = takeTaskAttemptList.OrderBy(dt => dt.distance).ToList();
        HashSet<Unit> unitsWhoHaveHadTaskTaken = new HashSet<Unit>();
        HashSet<UnitAndRefused> unitsWhoHaveTakenTask = new HashSet<UnitAndRefused>();

        foreach(DistanceAndTask distanceAndTask in sortedDistances) {
            Unit performingUnit = distanceAndTask.performingUnit;
            UnitAndRefused requester = distanceAndTask.requestingUnit;

            // A unit can only lose one task, and a requester can only take one
            if(unitsWhoHaveHadTaskTaken.Contains(performingUnit) || unitsWhoHaveTakenTask.Contains(requester)) {
                continue;
            }

            // Re-validate: units can die and tasks can finish between the path request and now
            if(performingUnit == null || requester.unit == null || performingUnit.currentMasterTask != distanceAndTask.masterTask) {
                continue;
            }

            try {
                performingUnit.CancelTask();
                requester.callback(distanceAndTask.masterTask);
            } catch(Exception ex) {
                Debug.LogException(ex);
                ReturnUnassignedTask(distanceAndTask.masterTask);
            }

            unitsWhoHaveHadTaskTaken.Add(performingUnit);
            unitsWhoHaveTakenTask.Add(requester);

            unitDistanceListMap.Remove(requester.unit);
            unitsRequestingTasks.Remove(requester);
        }
    }

    // Give the unit with the closest viable task its task, remove that task from any others down the list, and do it all again
    private void GiveOutQueuedTasks(Dictionary<Unit, UnitsDistanceList> unitDistanceListMap, HashSet<int> exhaustedTaskNumbers) {
        while (unitDistanceListMap.Count > 0) {
            List<UnitsDistanceList> unitDistanceListList = unitDistanceListMap.Values.ToList();

            unitDistanceListList.Sort(delegate (UnitsDistanceList t1, UnitsDistanceList t2) {
                return t1.shortestTaskDistance.CompareTo(t2.shortestTaskDistance);
            });

            UnitsDistanceList unitsDistanceList = unitDistanceListList[0];
            UnitAndRefused receiver = unitsDistanceList.unitAndRefused;

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

            // Remove the unit from the list of people with viable tasks AND from the list of requesters, then give it the task
            unitDistanceListMap.Remove(receiver.unit);
            unitsRequestingTasks.Remove(receiver);

            try {
                receiver.callback(givingTask);
            } catch(Exception ex) {
                Debug.LogException(ex);
                ReturnUnassignedTask(givingTask);
            }

            NotifyDelegates(givingTask.actionType);
        }
    }

    // If handing a task to a unit failed before the unit took it, put it back so it is not lost
    private void ReturnUnassignedTask(MasterGameTask task) {
        if(task != null && task.assignedUnit == null && !taskListMap[task.actionType].Contains(task)) {
            PutBackTask(task);
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
