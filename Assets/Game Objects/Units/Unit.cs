using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TaskStatusNotifiable {
    void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate);
    void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate);
}

public interface TaskStatusUpdateDelegate {
    void NowPerformingTask(Unit unit, MasterGameTask masterGameTask, GameTask gameTask);
}

public interface UserActionNotifiable {
    void RegisterForUserActionNotifications(UserActionUpdateDelegate notificationDelegate);
    void EndUserActionNotifications(UserActionUpdateDelegate notificationDelegate);
}

public interface UserActionUpdateDelegate {
    void UpdateUserActionsAvailable(UserAction[] userActions);
}

public static class UnitStateExtensions {
    public static string decription(this Unit.UnitState unitState) {
        switch(unitState) {
            case Unit.UnitState.Idle:
                return "Idle";
            case Unit.UnitState.Efficient:
                return "Efficient";
            case Unit.UnitState.Inefficient:
                return "Inefficient";
        }

        return "???";
    }

    public static int ranking(this Unit.UnitState unitState) {
        switch(unitState) {
            case Unit.UnitState.Idle:
                return 0;
            case Unit.UnitState.Inefficient:
                return 1;
            case Unit.UnitState.Efficient:
                return 2;
        }

        return -1;
    }

    static Color idleColor = new Color(0.4811321f, 0.388083f, 0.388083f);
    public static Color ColorForState(this Unit.UnitState unitState) {        

        switch(unitState) {
            case Unit.UnitState.Idle:
                return idleColor;
            case Unit.UnitState.Efficient:
                return Color.white;
            case Unit.UnitState.Inefficient:
                return Color.yellow;
        }    

        return Color.white;
    }
}

// Unit is an actionable item when it is being built, other units can take actions on it by dropping resources and building it.
public abstract class Unit : MonoBehaviour, Selectable, TerrainUpdateDelegate, Followable {

    public enum UnitState {
        Idle, Efficient, Inefficient
    }

    // Initialization
    [HideInInspector]
    public bool initialized { get; private set; }

    // Manager References
    TaskQueueManager taskQueueManager;
    PlayerBehaviour playerBehaviour;

    // Pathfinding
    public float speed;
    public float turnSpeed;
    public float followPathTurnSpeed;
    public float turnDistance;
    public float stoppingDistance;

    public Transform oreLocation;

    //Path path;
    Path pathToDraw;
    bool navigatingToTask;

    // Status Tooltip
    public Transform statusLocation;
    UnitStatusTooltip unitStatusTooltip;

    // Tasks
    public MasterGameTask currentMasterTask { get; private set; }

    // The queue of all tasks to do for the current Master Task
    Queue<GameTask> gameTasksQueue;
    GameTask currentGameTask; // The current Game Task we are working on to complete the Master Task
    private HashSet<int> refuseTaskSet; // Set of tasks we aready know we cannot perform

    public static int maxUnitUduration = 480;
    abstract public int duration { get; }
    public int remainingDuration = maxUnitUduration;
    abstract public MasterGameTask.ActionType primaryActionType { get; }

    abstract public float SpeedForTask(MasterGameTask.ActionType actionType);

    public UnitBuilding buildableComponent;

    public List<TaskStatusUpdateDelegate> taskStatusDelegateList = new List<TaskStatusUpdateDelegate>();
    public List<UserActionUpdateDelegate> userActionDelegateList = new List<UserActionUpdateDelegate>();

    [HideInInspector]

    /*
     * Selectable Interface Properties
     * */

    public string title { get; private set; }
    public string description => title;

    /*
     * Followable Interface
     * */
    public Transform followCameraLocation;

    public Transform followTransform {
        get {
            if (followCameraLocation != null) {
                return followCameraLocation;
            } else {
                GameObject camerLocationObject = new GameObject();
                camerLocationObject.transform.SetParent(transform);
                camerLocationObject.transform.localPosition = new Vector3(0f, 1.5f, -1.5f);
                camerLocationObject.transform.localRotation = Quaternion.identity;

                return camerLocationObject.transform;
            }            
        }
    }

    public Transform lookAtTransform => statusLocation;

    /*
     * Lifecycle
     * */ 

    private void Awake() {
        initialized = false;

        title = primaryActionType.TitleAsNoun() + " #" + UnitManager.unitCount[primaryActionType].ToString();
        UnitManager.unitCount[primaryActionType]++;

        gameTasksQueue = new Queue<GameTask>();
        refuseTaskSet = new HashSet<int>();

        unitStatusTooltip = Instantiate(Resources.Load("UI/UnitStatusPanel", typeof(UnitStatusTooltip))) as UnitStatusTooltip;
        unitStatusTooltip.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>());

        unitStatusTooltip.toFollow = statusLocation;

        unitStatusTooltip.SetTitle(title);
        unitStatusTooltip.SetTask(this, null);
        unitStatusTooltip.DisplayPercentageBar(false);
        unitStatusTooltip.SetRemainingDuration(duration, (float) duration / (float) maxUnitUduration);

    }

    private void OnDestroy() {
        try {
            Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
        } catch(System.NullReferenceException e) { }
    }

    public void Initialize() {
        initialized = true;

        // Register
        UnitManager unitManager = Script.Get<UnitManager>();
        unitManager.RegisterUnit(this);
        Script.Get<MapsManager>().AddTerrainUpdateDelegate(this);    
        playerBehaviour = Script.Get<PlayerBehaviour>();

        // Name
        Name name = NameSingleton.sharedInstance.GenerateName();

        NotificationPanel notificationManager = Script.Get<NotificationPanel>();
        notificationManager.AddNotification(new NotificationItem(title + " initialized and named: " + name.fullName, transform));

        unitStatusTooltip.SetTitle(name.shortform);
        title = name.fullName;

        // Duration
        this.remainingDuration = duration;
        Action<int, float> durationUpdateBlock = (remainingTime, percentComplete) => {
            this.remainingDuration = remainingTime;
            float percentOfMaxUnitTime = (float) remainingTime / (float) maxUnitUduration;

            unitStatusTooltip.SetRemainingDuration(remainingTime, percentOfMaxUnitTime);

            if (remainingTime == NotificationPanel.unitDurationWarning) {
                Script.Get<NotificationPanel>().AddNotification(new NotificationItem(primaryActionType.TitleAsNoun() + " " + name.shortform + " only has " + NotificationPanel.unitDurationWarning.ToString() + "s remaining.", transform));
            }
        };

        // Shutdown
        Action durationCompletionBlock = () => {
            Script.Get<NotificationPanel>().AddNotification(new NotificationItem(primaryActionType.TitleAsNoun() + " " + name.shortform + " has run out of power", transform));

            unitManager.DisableUnit(this);
            Shutdown();
        };

        Script.Get<TimeManager>().AddNewTimer(duration, durationUpdateBlock, durationCompletionBlock);
        
        // Setup Task Pipeline
        taskQueueManager = Script.Get<TaskQueueManager>();

        completedTaskAction = (pathComplete) => {
            unitStatusTooltip.DisplayPercentageBar(false);
            ContinueGameTaskQueue();
        };

        completedPath = (pathComplete) => {
            navigatingToTask = false;

            // Don't bother showing completion bar for picking up and droping off
            if(currentGameTask.action != GameTask.ActionType.DropOff && currentGameTask.action != GameTask.ActionType.PickUp) {
                unitStatusTooltip.DisplayPercentageBar(true);
            }        
            
            StartCoroutine(PerformTaskAction(completedTaskAction));
        };

        foundWaypoints = (waypoints, actionableItem, success) => {
            StopAllCoroutines();

            if(success) {                              
                // When requesting a path for an unknown resource (like ore) we will get the closest resource back as an actionable item
                if(actionableItem != null) {
                    currentGameTask.actionItem = actionableItem;
                }

                // If the task item is a known, like a location or builing, the actionItem was set at initialization
                // If the task item was an unknown resource, it has just been set above

                // In the first case, I want to let the the item know that this Master Task has an assigned unit
                // In the second, we need to alert the unknown resource that it has a new task associated
                currentGameTask.actionItem.AssociateTask(currentMasterTask);

                Path path = new Path(waypoints, transform.position, turnDistance, stoppingDistance, currentGameTask.target);
                pathToDraw = path;


                WorldPosition worldPos = currentGameTask.target;
                MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPos);

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

                print("Target At: " + layoutCoordinate.description);


                StartCoroutine(FollowPath(path, completedPath));
            } else {
                // There is no path to task, we cannot do this.
                print("Give up Task" + currentMasterTask.taskNumber);
                print("Put task back in Queue");

                refuseTaskSet.Add(currentMasterTask.taskNumber);

                taskQueueManager.PutBackTask(currentMasterTask);
                ResetTaskState();
            }
        };

        ResetTaskState();
        //StartCoroutine(FindTask());
    }

    public UnitState GetUnitState() {
        Unit.UnitState unitState = Unit.UnitState.Idle;

        if(currentMasterTask != null) {
            if(currentMasterTask.actionType == primaryActionType) {
                unitState = Unit.UnitState.Efficient;
            } else {
                unitState = Unit.UnitState.Inefficient;
            }
        }

        return unitState;
    }

    /*
     * Task Pipeline
     * */

    Action<bool> completedTaskAction;
    Action<bool> completedPath;
    Action<WorldPosition[], ActionableItem, bool> foundWaypoints;

    private void DoTask(MasterGameTask task) {
        currentMasterTask = task;
        currentMasterTask.assignedUnit = this;

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
            unitStatusTooltip.SetTask(this, currentGameTask);

            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        } else {
            currentMasterTask.MarkTaskFinished();
            ResetTaskState();
        }

        // Let the UI Know our status is changing
        NotifyAllTaskStatus();
    }

    public void CancelTask() {
        if(currentMasterTask == null) {
            return;
        }

        InterruptInProgressActions();
        ResetTaskState();
    }

    private void Shutdown() {
        taskQueueManager.RestractTaskRequest(this);
        InterruptInProgressActions();

        // Put back current task
        if(currentMasterTask != null) {
            taskQueueManager.PutBackTask(currentMasterTask);
        }


        buildableComponent.SetTransparentShaders();
        buildableComponent.SetAlphaSolid();

        Action<int, float> fadeOutBlock = (seconds, percent) => {
            buildableComponent.SetAlphaPercentage(1 - percent);
        };

        Action destroyBlock = () => {
            transform.SetParent(null);
            Destroy(unitStatusTooltip.gameObject);
            Destroy(gameObject);
        };

        Script.Get<TimeManager>().AddNewTimer(3, fadeOutBlock, destroyBlock, 2);
    }

    // Pipeline Helpers

    private void ResetTaskState() {
        currentMasterTask = null;
        currentGameTask = null;

        gameTasksQueue.Clear();

        NotifyAllTaskStatus();
        unitStatusTooltip.SetTask(this, null);
        unitStatusTooltip.DisplayPercentageBar(false);

        taskQueueManager.RequestNextDoableTask(this, (masterGameTask) => {
            DoTask(masterGameTask);
        }, refuseTaskSet);
    }

    private void InterruptInProgressActions() {
        StopAllCoroutines();

        // Our current task no longer has an associated action
        if(currentGameTask != null && currentGameTask.actionItem != null) {
            currentGameTask.actionItem.AssociateTask(null);
        }

        // All resources we are carrying get put back
        Script.Get<GameResourceManager>().ReturnAllToEnvironment(this);
    }

    /*
     * Task Coroutines
     * */

    protected abstract void Animate();

    IEnumerator PerformTaskAction(Action<bool> callBack) {

        float speed = SpeedForTask(currentMasterTask.actionType);

        while (true) {

            // Don't perform on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            float completion = currentGameTask.actionItem.performAction(currentGameTask, Time.deltaTime * speed, this);
            unitStatusTooltip.percentageBar.SetPercent(completion);

            Animate();

            if (completion >= 1) {
                callBack(true);
                yield break;
            }

            yield return null;
        }     
    }

    IEnumerator FollowPath(Path path, System.Action<bool> callBack) {

        int pathIndex = 0;
        bool turningToStart = true;

        Vector3 startPoint = path.lookPoints[0].vector3;
        Quaternion originalRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(startPoint - transform.position);

        float totalTurnDistance = 0;
        float degreesToTurn = (targetRotation.eulerAngles - originalRotation.eulerAngles).magnitude;
        print("Turn Degrees Magnitude " + degreesToTurn);

        while(turningToStart) {

            // Don't move on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            if(totalTurnDistance == 1) {
                turningToStart = false;
            } else {
                totalTurnDistance = Mathf.Clamp01(totalTurnDistance + ((Time.deltaTime * turnSpeed) / degreesToTurn * 180));
                transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, totalTurnDistance);
            }

            yield return null;
        }
        
        bool followingPath = true;

        // Used to slow as we approach target
        float speedPercent = 1;

        while(followingPath) {

            // Don't move on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            Vector2 position2D = transform.position.ToVector2();
            while(path.turnBoundaries[pathIndex].HasCrossedLine(position2D)) {
                //print("Crossed Path Boundaries");

                if(pathIndex == path.finishLineIndex) {
                    followingPath = false;
                    break;
                } else {
                    pathIndex++;

                    WorldPosition worldPos = path.lookPoints[pathIndex];
                    MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPos);

                    LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

                    //print("New Lookpoint at: " + worldPos.description + ", " + mapCoordinate.description + ", " + layoutCoordinate.description);
                }
            }

            if(followingPath) {
                if(pathIndex >= path.slowDownIndex && stoppingDistance > 0) {
                    speedPercent = Mathf.Clamp(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(position2D) / stoppingDistance, 0.15f, 1);
                }               

                WorldPosition lookPointWorldPos = path.lookPoints[pathIndex];
                MapCoordinate lookPointMapCoordinate = MapCoordinate.FromWorldPosition(lookPointWorldPos);

                WorldPosition playerWorldPos = new WorldPosition(transform.position);
                MapCoordinate playerMapCoordinate = MapCoordinate.FromWorldPosition(playerWorldPos);

                LayoutCoordinate playerLayoutCoordinate = new LayoutCoordinate(playerMapCoordinate);
                TerrainType currentTerrain = playerLayoutCoordinate.mapContainer.map.GetTerrainAt(playerLayoutCoordinate);

                float localSpeed = currentTerrain.walkSpeedMultiplier * speed;

                float height = Script.Get<MapsManager>().GetHeightAt(lookPointMapCoordinate) * lookPointMapCoordinate.mapContainer.transform.lossyScale.y; //  + (0.5f * transform.localScale.y)
                Vector3 lookPoint = new Vector3(lookPointWorldPos.vector3.x, height, lookPointWorldPos.vector3.z);

                targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
                transform.rotation =  Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * followPathTurnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * localSpeed * speedPercent, Space.Self);
            }

            yield return null;
        }

        bool lookingAtTarget = true;

        originalRotation = transform.rotation;
        Vector3 finalLookAtBalancedHeight = path.finalLookAt.vector3;
        finalLookAtBalancedHeight.y = transform.position.y;

        targetRotation = Quaternion.LookRotation(finalLookAtBalancedHeight - transform.position);

        totalTurnDistance = 0;
        degreesToTurn = (targetRotation.eulerAngles - originalRotation.eulerAngles).magnitude;


        while(lookingAtTarget) {
            // Don't move on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            if(totalTurnDistance == 1) {
                lookingAtTarget = false;

                callBack(true);
                yield break; // Stop Coroutine
            } else {
                totalTurnDistance = Mathf.Clamp01(totalTurnDistance + ((Time.deltaTime * turnSpeed) / degreesToTurn * 180));
                transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, totalTurnDistance);
            }

            yield return null;
        }
    }

    public void OnDrawGizmos() {
        if (pathToDraw != null) {
            pathToDraw.DrawWithGizmos();
        }
    }

    /*
     * TerrainUpdateDelegate Interface
     * */

    public void NotifyTerrainUpdate(LayoutCoordinate layoutCoordinate) {
        refuseTaskSet.Clear();

        if (currentMasterTask != null && navigatingToTask == true && currentGameTask.target.vector3 != this.transform.position) {
            // Request a new path if the world has updated and we are already on the move
            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        }
    }

    /*
     * Selectable Interface
     * */

    public void SetSelected(bool selected) {

        Color color = Color.white;
        if(selected) {
            color = PlayerBehaviour.tintColor;
        }

        foreach(Building.MeshBuildingTier meshTier in buildableComponent.meshTiers) {
            foreach(MeshRenderer renderer in meshTier.meshRenderes) {
                renderer.material.SetColor("_Color", color);
            }
        }
    }

    /*
     * TaskStatusNotifiable Interface
     * */

    public void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Add(notificationDelegate);

        // Let the subscriber know our status immediately
        notificationDelegate.NowPerformingTask(this, currentMasterTask, currentGameTask);
    }

    public void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Remove(notificationDelegate);
    }

    private void NotifyAllTaskStatus() {
        foreach(TaskStatusUpdateDelegate updateDelegate in taskStatusDelegateList.ToArray()) {
            updateDelegate.NowPerformingTask(this, currentMasterTask, currentGameTask);
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

    /*
     * Blueprints
     * */

    public class Blueprint : ConstructionBlueprint {
        private static string folder = "Units/";

        public static Blueprint Miner = new Blueprint("Miner", typeof(Miner), "MinerIcon", "Miner", new BlueprintCost(5, 3, 1));
        public static Blueprint Mover = new Blueprint("Mover", typeof(Mover), "MoverIcon", "Mover", new BlueprintCost(3, 1, 0));
        public static Blueprint Builder = new Blueprint("Builder", typeof(Builder), "BuilderIcon", "Builder", new BlueprintCost(5, 3, 1));

        public Blueprint(string fileName, Type type, string iconName, string label, BlueprintCost cost) : base(folder + fileName, type, iconName, label, cost) { }

        public override GameObject ConstructAt(LayoutCoordinate layoutCoordinate) {
            UnitManager unitManager = Script.Get<UnitManager>();
            Unit unit = UnityEngine.Object.Instantiate(resource) as Unit;

            UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();

            unitManager.BuildAt(unitBuilding, layoutCoordinate, cost);

            return unit.gameObject;
        }
    }
}
