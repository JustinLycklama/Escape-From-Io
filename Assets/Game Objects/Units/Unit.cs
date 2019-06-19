using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TaskStatusNotifiable {
    void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate);
    void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate);
}

public interface TaskStatusUpdateDelegate {
    void NowPerformingTask(MasterGameTask masterGameTask, GameTask gameTask);
}

public interface UserActionNotifiable {
    void RegisterForUserActionNotifications(UserActionUpdateDelegate notificationDelegate);
    void EndUserActionNotifications(UserActionUpdateDelegate notificationDelegate);
}

public interface UserActionUpdateDelegate {
    void UpdateUserActionsAvailable(UserAction[] userActions);
}

public abstract class Unit : MonoBehaviour, Selectable, TerrainUpdateDelegate {

    public static int unitCount = 0;

    // TaskQueue Reference
    TaskQueueManager taskQueueManager;

    // Pathfinding
    public float speed;
    public float turnSpeed;
    public float turnDistance;
    public float stoppingDistance;

    Path path;
    bool navigatingToTask;

    // Status Tooltip
    public Transform statusLocation;
    UnitStatusTooltip unitStatusTooltip;

    // Tasks
    MasterGameTask currentMasterTask;

    // The queue of all tasks to do for the current Master Task
    Queue<GameTask> gameTasksQueue;
    GameTask currentGameTask; // The current Game Task we are working on to complete the Master Task

    abstract public MasterGameTask.ActionType primaryActionType { get; }

    abstract public float SpeedForTask(GameTask task);

    /*
     * Selectable Interface Properties
     * */

    private string title;
    public string description => title;

    public List<TaskStatusUpdateDelegate> taskStatusDelegateList = new List<TaskStatusUpdateDelegate>();
    public List<UserActionUpdateDelegate> userActionDelegateList = new List<UserActionUpdateDelegate>();

    /*
     * Lifecycle
     * */ 

    private void Awake() { 
        title = "Unit #" + unitCount;
        unitCount++;

        gameTasksQueue = new Queue<GameTask>();  
    }

    private void Start() {
        unitStatusTooltip = Instantiate(Resources.Load("UnitStatusPanel", typeof(UnitStatusTooltip))) as UnitStatusTooltip;
        unitStatusTooltip.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>());

        unitStatusTooltip.SetFollower(statusLocation);

        unitStatusTooltip.SetTitle(title);
        unitStatusTooltip.SetTask(null);
        unitStatusTooltip.DisplayPercentageBar(false);
    }

    private void OnDestroy() {
        Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
    }

    public void Initialize() {
        taskQueueManager = Script.Get<TaskQueueManager>();
        Script.Get<MapsManager>().AddTerrainUpdateDelegate(this);

        completedTaskAction = (pathComplete) => {
            unitStatusTooltip.DisplayPercentageBar(false);
            ContinueGameTaskQueue();
        };

        completedPath = (pathComplete) => {
            print("Do Action for Task" + currentMasterTask.taskNumber);
            navigatingToTask = false;
            unitStatusTooltip.DisplayPercentageBar(true);
            StartCoroutine(PerformTaskAction(completedTaskAction));
        };

        foundWaypoints = (waypoints, actionableItem, success) => {
            StopAllCoroutines();

            if(success) {
                print("Follow Path for Task" + currentMasterTask.taskNumber);

                path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);

                // When requesting a path for an unknown resource (like ore) we will get the closest resource back as an actionable item
                if(actionableItem != null) {
                    currentGameTask.actionItem = actionableItem;
                }

                // If the task item is a known, like a location or builing, the actionItem was set at initialization
                // If the task item was an unknown resource, it has just been set above

                // In the first case, I want to let the the item know that this Master Task has an assigned unit
                // In the second, we need to alert the unknown resource that it has a new task associated
                currentGameTask.actionItem.AssociateTask(currentMasterTask);


                StartCoroutine(FollowPath(completedPath));
            } else {
                // There is no path to task, we cannot do this.
                print("Give up Task" + currentMasterTask.taskNumber);
                print("Put task back in Queue");

                taskQueueManager.QueueTask(currentMasterTask);
                ResetTaskState();
            }
        };

        StartCoroutine(FindTask());
    }

    public void CancelTask() {
        if (currentMasterTask == null) {
            return;
        }

        StopAllCoroutines();
        ResetTaskState();
    }

    /*
     * Task Pipeline
     * */

    System.Action<bool> completedTaskAction;
    System.Action<bool> completedPath;
    System.Action<WorldPosition[], ActionableItem, bool> foundWaypoints;

    private void DoTask(MasterGameTask task) {
        currentMasterTask = task;
        currentMasterTask.assignedUnit = this;

        unitStatusTooltip.SetTask(currentMasterTask);

        gameTasksQueue.Clear();

        //navigatingToTask = true;
        foreach (GameTask gameTask in currentMasterTask.childGameTasks) {
            gameTasksQueue.Enqueue(gameTask);
        }

        ContinueGameTaskQueue();
    }

    private void ContinueGameTaskQueue() {

        if (gameTasksQueue.Count > 0) {
            currentGameTask = gameTasksQueue.Dequeue();
        
            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        } else {
            print("Complpete Task" + currentMasterTask.taskNumber);

            currentMasterTask.MarkTaskFinished();
            ResetTaskState();
        }

        // Let the UI Know our status is changing
        NotifyAllTaskStatus();
    }

    private void ResetTaskState() {
        currentMasterTask = null;
        currentGameTask = null;

        gameTasksQueue.Clear();

        NotifyAllTaskStatus();
        unitStatusTooltip.SetTask(null);
        unitStatusTooltip.DisplayPercentageBar(false);

        StartCoroutine(FindTask());
    }

    /*
     * Task Coroutines
     * */

    IEnumerator FindTask() {
        MasterGameTask masterGameTask = null;

        while(masterGameTask == null) {
            yield return new WaitForSeconds(0.25f);

            masterGameTask = taskQueueManager.GetNextDoableTask(this);            
        }

        DoTask(masterGameTask);
    }

    IEnumerator PerformTaskAction(System.Action<bool> callBack) {

        float speed = SpeedForTask(currentGameTask);

        while (true) {
            float completion = currentGameTask.actionItem.performAction(currentGameTask, Time.deltaTime * speed, this);
            unitStatusTooltip.percentageBar.SetPercent(completion);

            if (completion >= 1) {
                callBack(true);
                yield break;
            }

            yield return null;
        }     
    }

    IEnumerator FollowPath(System.Action<bool> callBack) {

        bool followingPath = true;
        int pathIndex = 0;

        Vector3 startPoint = path.lookPoints[0].vector3;

        transform.LookAt(new Vector3(startPoint.x, transform.position.y, startPoint.z));

        // Used to slow as we approach target
        float speedPercent = 1;

        while(followingPath) {
            Vector2 position2D = transform.position.ToVector2();
            while(path.turnBoundaries[pathIndex].HasCrossedLine(position2D)) {
                if(pathIndex == path.finishLineIndex) {
                    followingPath = false;

                    callBack(true);
                    yield break; // Stop Coroutine
                } else {
                    pathIndex++;
                }
            }

            if(followingPath) {
                if(pathIndex >= path.slowDownIndex && stoppingDistance > 0) {
                    speedPercent = Mathf.Clamp(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(position2D) / stoppingDistance, 0.15f, 1);
                }               

                WorldPosition worldPos = path.lookPoints[pathIndex];
                MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPos);

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
                TerrainType currentTerrain = layoutCoordinate.mapContainer.map.GetTerrainAt(layoutCoordinate);

                float localSpeed = currentTerrain.walkSpeedMultiplier * speed;

                float height = Script.Get<MapsManager>().GetHeightAt(mapCoordinate) * mapCoordinate.mapContainer.transform.localScale.y + (0.5f * transform.localScale.y);
                Vector3 lookPoint = new Vector3(worldPos.vector3.x, height, worldPos.vector3.z);

                Quaternion targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
            }

            yield return null;
        }
    }

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
        }
    }

    /*
     * TerrainUpdateDelegate Interface
     * */

    public void NotifyTerrainUpdate() {
        if (currentMasterTask != null && navigatingToTask == true && currentGameTask.target.vector3 != this.transform.position) {
            // Request a new path if the world has updated and we are already on the move
            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        }
    }

    /*
     * Selectable Interface
     * */

    public void SetSelected(bool selected) {

        Color tintColor = selected ? Color.cyan : Color.white;
        gameObject.GetComponent<MeshRenderer>().material.color = tintColor;
    }

    /*
     * TaskStatusNotifiable Interface
     * */

    public void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Add(notificationDelegate);

        // Let the subscriber know our status immediately
        notificationDelegate.NowPerformingTask(currentMasterTask, currentGameTask);
    }

    public void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Remove(notificationDelegate);
    }

    public void NotifyAllTaskStatus() {
        foreach(TaskStatusUpdateDelegate updateDelegate in taskStatusDelegateList) {
            updateDelegate.NowPerformingTask(currentMasterTask, currentGameTask);
        }
    }

    /*
     * UserActionNotifiable Interface
     * */

    public void RegisterForUserActionNotifications(UserActionUpdateDelegate notificationDelegate) {
        userActionDelegateList.Add(notificationDelegate);

        // Let the subscriber know our status immediately
        notificationDelegate.UpdateUserActionsAvailable(null);
    }

    public void EndUserActionNotifications(UserActionUpdateDelegate notificationDelegate) {
        userActionDelegateList.Remove(notificationDelegate);
    }

    public void NotifyAllUserActionsUpdate() {
        foreach(UserActionUpdateDelegate updateDelegate in userActionDelegateList) {
            updateDelegate.UpdateUserActionsAvailable(null);
        }
    }
}
