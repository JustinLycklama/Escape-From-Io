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
public abstract class Unit : ActionableItem, Selectable, TerrainUpdateDelegate, Followable, TaskLockUpdateDelegate {

    public enum UnitState {
        Idle, Efficient, Inefficient
    }

    public enum FactionType { Player, Enemy }

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

    public int movementPenaltyMultiplier = 1;
    public float unitSpeed {
        get {
            return speed * ResearchSingleton.sharedInstance.unitSpeedMultiplier;
        }
    }


    public Transform oreLocation;

    //Path path;
    protected Path pathToDraw;
    protected bool navigatingToTask;

    // Status Tooltip
    public Transform statusLocation;
    protected UnitStatusTooltip unitStatusTooltip;

    // Tasks
    public float relativeDistanceToTask;
    public MasterGameTask currentMasterTask { get; protected set; }

    // The queue of all tasks to do for the current Master Task
    protected Queue<GameTask> gameTasksQueue;

    public GameTask takeableTask {
        get {
            return currentGameTask;
        }
    }

    public bool canTakeTaskFromUnit {
        get {
            return currentMasterTask != null && currentGameTask != null && currentGameTask == currentMasterTask.childGameTasks[0] && currentPercentOfJournery < 1 && currentMasterTask.actionType != MasterGameTask.ActionType.Move;
        }
    }

    public float remainingMovementCostOnTask {
        get {
            return movementCostToTask * (1 - currentPercentOfJournery) * unitSpeed;
        }
    }

    public virtual FactionType factionType { get { return FactionType.Player; } }

    private int movementCostToTask = 0;
    private float currentPercentOfJournery = 0;
    protected GameTask currentGameTask; // The current Game Task we are working on to complete the Master Task
    private HashSet<int> refuseTaskSet; // Set of tasks we aready know we cannot perform

    public static int maxUnitUduration = 600;

    abstract public int duration { get; }
    public int unitDuration { get { return duration + ResearchSingleton.sharedInstance.unitDurationAddition; } }
    public int unitHealth = 100;

    [HideInInspector]
    public int remainingDuration = maxUnitUduration;

    [HideInInspector]
    public int remainingHealth;

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
    public override string description => title;

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
        unitStatusTooltip.SetRemainingDuration(unitDuration, (float)unitDuration / (float) maxUnitUduration);
        unitStatusTooltip.SetRemainingHealth(unitHealth, 1.0f);
    }

    private void OnDestroy() {
        try {
            Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
            taskQueueManager.EndLockUpdates(this);
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

        unitStatusTooltip.SetTitle(primaryActionType.TitleAsNoun());
        title = name.fullName;

        // Duration
        this.remainingDuration = unitDuration;
        Action<int, float> durationUpdateBlock = (remainingTime, percentComplete) => {
            this.remainingDuration = remainingTime;
            float percentOfMaxUnitTime = (float) remainingTime / (float) maxUnitUduration;

            unitStatusTooltip?.SetRemainingDuration(remainingTime, percentOfMaxUnitTime);

            if (remainingTime == NotificationPanel.unitDurationWarning) {
                Script.Get<NotificationPanel>().AddNotification(new NotificationItem(primaryActionType.TitleAsNoun() + " " + name.shortform + " only has " + NotificationPanel.unitDurationWarning.ToString() + "s remaining.", transform));
            }
        };

        // Health
        unitHealth = 100;
        remainingHealth = unitHealth;

        // Shutdown
        Action durationCompletionBlock = () => {
            Script.Get<NotificationPanel>().AddNotification(new NotificationItem(primaryActionType.TitleAsNoun() + " " + name.shortform + " has run out of power", transform));

            unitManager.DisableUnit(this);
            Shutdown();
        };

        Script.Get<TimeManager>().AddNewTimer(unitDuration, durationUpdateBlock, durationCompletionBlock);
        
        // Setup Task Pipeline
        taskQueueManager = Script.Get<TaskQueueManager>();
        taskQueueManager.RegisterForLockStatusUpdates(this);

        completedTaskAction = (pathComplete) => {
            unitStatusTooltip.DisplayPercentageBar(false);
            ContinueGameTaskQueue();
        };

        completedPath = (pathComplete) => {
            navigatingToTask = false;

            // Don't bother showing completion bar for picking up and droping off
            if(currentGameTask!= null && currentGameTask.action != GameTask.ActionType.DropOff && currentGameTask.action != GameTask.ActionType.PickUp) {
                unitStatusTooltip.DisplayPercentageBar(true);
            }

            performTaskCoroutine = StartCoroutine(PerformTaskAction(completedTaskAction));
        };

        foundWaypoints = (waypoints, actionableItem, success, distance) => {
            StopActionCoroutines();

            if(success) {
                navigatingToTask = true;

                // When requesting a path for an unknown resource (like ore) we will get the closest resource back as an actionable item
                if(actionableItem != null) {
                    currentGameTask.actionItem = actionableItem;
                }

                movementCostToTask = distance;
                //print("Path Distance " + distance);            

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

                FollowPathCoroutine = StartCoroutine(FollowPath(path, completedPath));
            } else {
                // There is no path to task, we cannot do this.
                refuseTaskSet.Add(currentMasterTask.taskNumber);

                taskQueueManager.PutBackTask(currentMasterTask);
                ResetTaskState();
            }
        };

        UnitCustomInit();
        ResetTaskState();        
    }

    protected abstract void UnitCustomInit();

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

    protected Action<bool> completedTaskAction;
    protected Action<bool> completedPath;
    protected Action<LookPoint[], ActionableItem, bool, int> foundWaypoints;

    protected PathRequest currentPathRequest;

    protected void DoTask(MasterGameTask task) {
        currentMasterTask = task;
        currentMasterTask.assignedUnit = this;

        gameTasksQueue.Clear();

        foreach (GameTask gameTask in currentMasterTask.childGameTasks) {
            gameTasksQueue.Enqueue(gameTask);
        }

        ContinueGameTaskQueue();
    }

    protected void RequestPath(Vector3 position, int movementPenaltyMultiplier, GameTask task, Action<LookPoint[], ActionableItem, bool, int> callback) {

        if (currentPathRequest != null) {
            currentPathRequest.Cancel();
        }

        currentPathRequest = PathRequestManager.RequestPathForTask(transform.position, movementPenaltyMultiplier, currentGameTask, (waypoints, actionableItem, success, distance) => {
            currentPathRequest = null;
            callback(waypoints, actionableItem, success, distance);
        });
    }

    protected void ContinueGameTaskQueue() {

        if (gameTasksQueue.Count > 0) {
            currentGameTask = gameTasksQueue.Dequeue();
            unitStatusTooltip.SetTask(this, currentGameTask);

            RequestPath(transform.position, movementPenaltyMultiplier, currentGameTask, foundWaypoints);
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

    public void DestroySelf() {
        deletionWatcher?.ObjectDeleted(this);

        transform.SetParent(null);

        Destroy(unitStatusTooltip.gameObject);
        unitStatusTooltip = null;

        Destroy(gameObject);
    }

    private void Shutdown() {
        taskQueueManager.RestractTaskRequest(this);
        InterruptInProgressActions();



        // Put back current task
        if(currentMasterTask != null) {
            taskQueueManager.PutBackTask(currentMasterTask);
        }

        Action<int, float> fadeOutBlock = null;

        if (buildableComponent != null) {
            buildableComponent.SetTransparentShaders();
            buildableComponent.SetAlphaSolid();

            fadeOutBlock = (seconds, percent) => {
                buildableComponent.SetAlphaPercentage(1 - percent);
            };
        }

        Action destroyBlock = () => {
            DestroySelf();
        };

        Script.Get<TimeManager>().AddNewTimer(3, fadeOutBlock, destroyBlock, 2);
    }

    // Pipeline Helpers

    protected virtual void ResetTaskState() {
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
        StopActionCoroutines();

        // Our current task no longer has an associated action
        if(currentGameTask != null && currentGameTask.actionItem != null) {
            currentGameTask.actionItem.AssociateTask(null);
        }

        // All resources we are carrying get put back
        Script.Get<GameResourceManager>().ReturnAllToEnvironment(this);
    }

    protected void StopActionCoroutines() {
        if (performTaskCoroutine != null) {
            StopCoroutine(performTaskCoroutine);
        }

        if(FollowPathCoroutine != null) {
            StopCoroutine(FollowPathCoroutine);
        }
    }

    /*
     * Task Coroutines
     * */
        
    // TODO: Combine Animate() with begin and completion delegates
    protected abstract void Animate();

    protected virtual void BeginTaskActionDelegate() { }
    protected virtual void CompleteTaskActionDelegate() { }

    protected Coroutine performTaskCoroutine;
    protected IEnumerator PerformTaskAction(Action<bool> callBack) {

        float speed = SpeedForTask(currentMasterTask.actionType) * ResearchSingleton.sharedInstance.unitActionMultiplier;

        BeginTaskActionDelegate();

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
                CompleteTaskActionDelegate();
                yield break;
            }

            yield return null;
        }     
    }

    protected virtual void BeginWalkDelegate() { }
    protected virtual void CompleteWalkDelegate() { }

    protected Coroutine FollowPathCoroutine;    
    protected IEnumerator FollowPath(Path path, System.Action<bool> callBack) {

        int pathIndex = 0;
        bool turningToStart = true;

        Vector3 startPoint = path.lookPoints[0].worldPosition.vector3;
        startPoint.y = transform.position.y;

        Quaternion originalRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(startPoint - transform.position);

        float totalTurnDistance = 0;
        float degreesToTurn = (targetRotation.eulerAngles - originalRotation.eulerAngles).magnitude;

        currentPercentOfJournery = 0;
        float basePercentOfJourney = 0;

        Vector3 previousWaypointPosition = transform.position;

        // If we are not moving at all (with some grey area) then don't bother turning to it.
        if (Vector3.Distance(startPoint, transform.position) < 5) {
            turningToStart = false;
        }

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

        // If we are only moving one space, and that space is very short, don't bother (fixes units spinning around when doing a task in same location)
        if (path.lookPoints.Length == 1 && Vector3.Distance(path.lookPoints[pathIndex].worldPosition.vector3, transform.position) < 5) {
            followingPath = false;
        }

        while(followingPath) {

            BeginWalkDelegate();

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
                    basePercentOfJourney += path.lookPoints[pathIndex].percentOfJourney;
                    previousWaypointPosition = path.lookPoints[pathIndex].worldPosition.vector3;
                    pathIndex++;

                    //WorldPosition worldPos = path.lookPoints[pathIndex].worldPosition;
                    //MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPos);

                    //LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
                    //print("New Lookpoint at: " + worldPos.description + ", " + mapCoordinate.description + ", " + layoutCoordinate.description);
                }
            }

            if(followingPath) {
                if(pathIndex >= path.slowDownIndex && stoppingDistance > 0) {
                    speedPercent = Mathf.Clamp(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(position2D) / stoppingDistance, 0.15f, 1);
                }

                WorldPosition lookPointWorldPos = path.lookPoints[pathIndex].worldPosition;
                MapCoordinate lookPointMapCoordinate = MapCoordinate.FromWorldPosition(lookPointWorldPos);

                WorldPosition playerWorldPos = new WorldPosition(transform.position);
                MapCoordinate playerMapCoordinate = MapCoordinate.FromWorldPosition(playerWorldPos);

                LayoutCoordinate playerLayoutCoordinate = new LayoutCoordinate(playerMapCoordinate);
                TerrainType currentTerrain = playerLayoutCoordinate.mapContainer.map.GetTerrainAt(playerLayoutCoordinate);          

                float localSpeed = Mathf.Pow(currentTerrain.walkSpeedMultiplier, movementPenaltyMultiplier) * unitSpeed;

                float height = Script.Get<MapsManager>().GetHeightAt(lookPointMapCoordinate) * lookPointMapCoordinate.mapContainer.transform.lossyScale.y; //  + (0.5f * transform.localScale.y)
                Vector3 lookPoint = new Vector3(lookPointWorldPos.vector3.x, height, lookPointWorldPos.vector3.z);

                targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
                transform.rotation =  Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * followPathTurnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * localSpeed * speedPercent, Space.Self);

                // Keep track of our % to reach our goal
                float totalDistanceFromGoal = Vector3.Distance(previousWaypointPosition, lookPoint);
                float currentDistanceFromGoal = Vector3.Distance(transform.position, lookPoint);

                currentPercentOfJournery = basePercentOfJourney + ((Mathf.InverseLerp(totalDistanceFromGoal, 0, currentDistanceFromGoal) * path.lookPoints[pathIndex].percentOfJourney));

                //print("Percent of Journey Done: " + currentPercentOfJournery);
            }

            yield return null;
        }

        CompleteWalkDelegate();

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
            RequestPath(transform.position, movementPenaltyMultiplier, currentGameTask, foundWaypoints);
        }
    }


    /*
     * Actionable Item
     * */

    float attackActionPercent = 0;
    float attackModifierSpeed = 1f;

    public override float performAction(GameTask task, float rate, Unit unit) {
        switch(task.action) {

            case GameTask.ActionType.Attack:
                attackActionPercent += rate * attackModifierSpeed;

                if(attackActionPercent >= 1) {
                    attackActionPercent = 1;

                    // The associatedTask is over
                    AssociateTask(null);
                    TakeDamage(5);

                    //GameResourceManager resourceManager = Script.Get<GameResourceManager>();
                    //resourceManager.GiveToUnit(this, unit);

                    attackActionPercent = 0;

                    return 1;
                }

                return attackActionPercent;
            default:
                break;
        }

        return 0;
    }

    private void TakeDamage(int damage) {

        remainingHealth -= damage;

        if (remainingHealth <= 0) {
            remainingHealth = 0;

            Script.Get<NotificationPanel>().AddNotification(new NotificationItem(primaryActionType.TitleAsNoun() + " " + primaryActionType.TitleAsNoun() + " has been destroyed", transform));
            Script.Get<UnitManager>().DisableUnit(this);
            Shutdown();
        }

        unitStatusTooltip.SetRemainingHealth(remainingHealth, (float)remainingHealth / (float)unitHealth);
    }

    /*
     * Selectable Interface
     * */

    public void SetSelected(bool selected) {

        Color color = Color.white;
        if(selected) {
            color = PlayerBehaviour.tintColor;
        }

        if (buildableComponent != null) {
            foreach(Building.MeshBuildingTier meshTier in buildableComponent.meshTiers) {
                foreach(MeshRenderer renderer in meshTier.meshRenderes) {
                    renderer.material.SetColor("_Color", color);
                }
            }
        }        
    }

    DeletionWatch deletionWatcher;
    public void SubscribeToDeletionWatch(DeletionWatch watcher) {
        deletionWatcher = watcher;
    }

    public void EndDeletionWatch(DeletionWatch watcher) {
        if (deletionWatcher == watcher) {
            deletionWatcher = null;
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

    protected void NotifyAllTaskStatus() {
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
     * TaskLockUpdateDelegate Interface
     * */

    public void NotifyUpdateLockState(MasterGameTask.ActionType actionType, bool locked) {
        // If we are locking a current list we should not have access to
        if (locked && currentMasterTask != null && currentMasterTask.actionType == actionType && actionType != primaryActionType) {
            InterruptInProgressActions();

            taskQueueManager.PutBackTask(currentMasterTask);

            ResetTaskState();
        }
    }

    /*
     * Blueprints
     * */

    public class Blueprint : ConstructionBlueprint {
        private static string folder = "Units/";

        public static Blueprint Miner = new Blueprint("Miner", typeof(Miner), "MinerIcon", "Miner", "A basic Mining Automaton.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 },
                { MineralType.Silver, 2 }                
            }));

        public static Blueprint Mover = new Blueprint("Mover", typeof(Mover), "MoverIcon", "Mover", "A basic Moving Automaton.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 }                                
            }));

        public static Blueprint Builder = new Blueprint("Builder", typeof(Builder), "BuilderIcon", "Builder", "A basic Building Automaton.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 },
                { MineralType.Silver, 2 }                
            }));

        public static Blueprint AdvancedMiner = new Blueprint("AdvancedMiner", typeof(AdvancedMiner), "MinerIcon", "Adv. Miner", "Faster at Mining than the basic.", 
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 3 },
                { MineralType.Gold, 2 }
            }),
            (LayoutCoordinate layoutCoordinate) => {
                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(AdvUnitBuilding));                
            },
            "Build Adjacent to " + Building.Blueprint.AdvUnitBuilding.label            
            );

        public static Blueprint AdvancedMover = new Blueprint("AdvancedMover", typeof(AdvancedMover), "MoverIcon", "Adv. Mover", "Hovering Mover.\nTerrain has no effect on this Unit.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 4}                
            }),
            (LayoutCoordinate layoutCoordinate) => {
                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(AdvUnitBuilding));
            },
            "Build Adjacent to " + Building.Blueprint.AdvUnitBuilding.label
            );

        public static Blueprint AdvancedBuilder = new Blueprint("AdvancedBuilder", typeof(AdvancedBuilder), "BuilderIcon", "Adv. Builder", "Faster at Building than the basic.",
            new BlueprintCost(new Dictionary<MineralType, int>(){                
                { MineralType.Silver, 3 },
                { MineralType.Gold, 2 }
            }),
            (LayoutCoordinate layoutCoordinate) => {
                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(AdvUnitBuilding));
            },
            "Build Adjacent to " + Building.Blueprint.AdvUnitBuilding.label
            );

        public Blueprint(string fileName, Type type, string iconName, string label, string description, BlueprintCost cost) : base(folder + fileName, type, iconName, label, description, cost) { }

        public Blueprint(string fileName, Type type, string iconName, string label, string description, BlueprintCost cost, Func<LayoutCoordinate, bool> requirementsMet, string notMetString) : 
            base(folder + fileName, type, iconName, label, description, cost, requirementsMet, notMetString) { }


        public override GameObject ConstructAt(LayoutCoordinate layoutCoordinate) {
            UnitManager unitManager = Script.Get<UnitManager>();
            Unit unit = UnityEngine.Object.Instantiate(resource) as Unit;

            UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();

            unitManager.BuildAt(unitBuilding, layoutCoordinate, cost);

            return unit.gameObject;
        }
    }
}
