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

    public static Color ColorForState(this Unit.UnitState unitState) {        

        switch(unitState) {
            case Unit.UnitState.Idle:
                return ColorSingleton.sharedInstance.idleUnitColor;
            case Unit.UnitState.Efficient:
                return ColorSingleton.sharedInstance.efficientColor;
            case Unit.UnitState.Inefficient:
                return ColorSingleton.sharedInstance.inefficientUnitColor;
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
    GameResourceManager resourceManager;

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
    public bool skipDurationUpdates = false;
    [HideInInspector]
    public int remainingDuration = maxUnitUduration;

    [HideInInspector]
    public int remainingHealth;

    abstract public MasterGameTask.ActionType primaryActionType { get; }

    abstract public float SpeedForTask(MasterGameTask.ActionType actionType);

    public UnitBuilding buildableComponent;

    public List<TaskStatusUpdateDelegate> taskStatusDelegateList = new List<TaskStatusUpdateDelegate>();
    public List<UserActionUpdateDelegate> userActionDelegateList = new List<UserActionUpdateDelegate>();

    protected Dictionary<string, int> coroutinesCount;

    [HideInInspector]

    /*
     * Selectable Interface Properties
     * */

    public string title { get; private set; }
    public override string description => title;

    public static string Unit_Noun = "Robot";

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

        title = primaryActionType.TitleAsNoun();
        UnitManager.unitCount[primaryActionType]++;

        gameTasksQueue = new Queue<GameTask>();
        refuseTaskSet = new HashSet<int>();

        coroutinesCount = new Dictionary<string, int>();
        
        foreach(string name in new string[] { "task", "path", "turn", "reset" }) {
            coroutinesCount[name] = 0;
        }

    }

    private void OnDestroy() {
        try {
            Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
            taskQueueManager.EndLockUpdates(this);
        } catch(NullReferenceException) { }
    }

    public void OnDrawGizmos() {
        if(pathToDraw != null) {
            pathToDraw.DrawWithGizmos();
        }
    }

    public void Initialize() {
        initialized = true;

        TutorialManager.sharedInstance.Fire(TutorialTrigger.UnitCompleted);

        // Register
        UnitManager unitManager = Script.Get<UnitManager>();
        unitManager.RegisterUnit(this);
        Script.Get<MapsManager>().AddTerrainUpdateDelegate(this);    
        playerBehaviour = Script.Get<PlayerBehaviour>();
        resourceManager = Script.Get<GameResourceManager>();

        // Tooltip
        unitStatusTooltip = Instantiate(Resources.Load("UI/UnitStatusPanel", typeof(UnitStatusTooltip))) as UnitStatusTooltip;
        unitStatusTooltip.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>(), true);

        unitStatusTooltip.followPosition = statusLocation;
        unitStatusTooltip.followingObject = transform;

        unitStatusTooltip.SetPrimaryActionAndFaction(primaryActionType, factionType);
        unitStatusTooltip.DisplayPercentageBar(false);
        unitStatusTooltip.SetRemainingDuration(unitDuration, (float)unitDuration / (float)maxUnitUduration);
        unitStatusTooltip.SetRemainingHealth(unitHealth, 1.0f);

        // Name
        //Name name = NameSingleton.sharedInstance.GenerateName();
        //title = name.fullName;


        NotificationPanel notificationManager = Script.Get<NotificationPanel>();
        if (factionType == FactionType.Player) {
            notificationManager.AddNotification(new NotificationItem($"{Unit_Noun} initialized", NotificationType.NewUnit, transform, primaryActionType));
        } else {
            notificationManager.AddNotification(new NotificationItem("Rock Golem Appeared!", NotificationType.NewEnemy, transform, primaryActionType));
        }
        
        // Duration
        this.remainingDuration = unitDuration;
        Action<int, float> durationUpdateBlock = (remainingTime, percentComplete) => {
            if (this == null || gameObject == null || !gameObject.activeSelf || skipDurationUpdates) {
                return;
            }

            SetRemainingDuration(remainingTime);
        };

        // Health
        unitHealth = 100;
        remainingHealth = unitHealth;

        // Shutdown
        Action durationCompletionBlock = () => {
            if(this == null || gameObject == null || !gameObject.activeSelf) {
                return;
            }

            Script.Get<NotificationPanel>().AddNotification(new NotificationItem($"{Unit_Noun} has run out of power", NotificationType.UnitBattery, transform, primaryActionType));

            Shutdown();
        };

        Script.Get<TimeManager>().AddNewTimer(unitDuration, durationUpdateBlock, durationCompletionBlock);
        
        // Setup Task Pipeline
        taskQueueManager = Script.Get<TaskQueueManager>();
        taskQueueManager.RegisterForLockStatusUpdates(this);

        completedTaskAction = (success) => {
            AnimateState(AnimationState.Idle);

            unitStatusTooltip.DisplayPercentageBar(false);
            ContinueGameTaskQueue();
        };

        completedPath = (pathComplete) => {
            navigatingToTask = false;
            AnimateState(AnimationState.Idle);

            // Only show progress for building, mining and cleaning
            if(currentGameTask != null && 
            (currentGameTask.action == GameTask.ActionType.Mine || currentGameTask.action == GameTask.ActionType.Build || currentGameTask.action == GameTask.ActionType.FlattenPath)) {
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

                StopActionCoroutines();
                followPathCoroutine = StartCoroutine(FollowPath(path, completedPath));
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
        currentMasterTask.SetAssignedUnit(this);

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
            unitStatusTooltip.SetTask(this, currentMasterTask, currentGameTask);

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

        // unitStatusTooltip created only on Initialize Robot
        if(unitStatusTooltip != null) {
            Destroy(unitStatusTooltip.gameObject);
            unitStatusTooltip = null;
        }

        TutorialManager.sharedInstance.Fire(TutorialTrigger.UnitFinishedDestroySelf);

        Destroy(gameObject);       
    }

    public virtual void Shutdown() {
        taskQueueManager.RestractTaskRequest(this);
        InterruptInProgressActions();

        AnimateState(AnimationState.Die);

        Script.Get<UnitManager>().DisableUnit(this);

        // Put back current task
        if(currentMasterTask != null) {
            taskQueueManager.PutBackTask(currentMasterTask);
        }

        Action<int, float> fadeOutBlock = null;

        if(buildableComponent != null) {
            buildableComponent.SetTransparentShaders();
            buildableComponent.SetAlphaSolid();

            fadeOutBlock = (seconds, percent) => {
                buildableComponent.SetAlphaPercentage(1 - percent);
            };
        }

        Action destroyBlock = () => {
            DestroySelf();
        };


        TimeManager timeManager = Script.Get<TimeManager>();
        
        // Allow time for showdown animation
        timeManager.AddNewTimer(2, null, () => {
            // Then fade and destroy object
            timeManager.AddNewTimer(3, fadeOutBlock, destroyBlock, 2);
        });


    }

    // Pipeline Helpers

    protected virtual void ResetTaskState() {
        currentMasterTask = null;
        currentGameTask = null;

        AnimateState(AnimationState.Idle);

        gameTasksQueue.Clear();

        NotifyAllTaskStatus();
        unitStatusTooltip.SetTask(this, null, null);
        unitStatusTooltip.DisplayPercentageBar(false);

        taskQueueManager.RequestNextDoableTask(this, (masterGameTask) => {
            DoTask(masterGameTask);
        }, refuseTaskSet);
    }

    private void InterruptInProgressActions() {
        StopActionCoroutines();

        unitStatusTooltip.DisplayPercentageBar(false);

        // Our current task no longer has an associated action
        if(currentGameTask != null && currentGameTask.actionItem != null) {
            currentGameTask.actionItem.AssociateTask(null);
        }

        // All resources we are carrying get put back
        resourceManager.ReturnAllToEnvironment(this);
    }

    protected void StopActionCoroutines() {
        if (performTaskCoroutine != null) {
            StopCoroutine(performTaskCoroutine);
        }

        if(followPathCoroutine != null) {
            StopCoroutine(followPathCoroutine);
        }

        if (turnCoroutine != null) {
            StopCoroutine(turnCoroutine);
        }
    }

    /*
     * Animation States
     * */

    public enum AnimationState {
        Idle, TurnLeft, TurnRight, Walk, WalkTurnRight, WalkTurnLeft, PerformCoreAction, Pickup, Die
    }

    protected abstract void AnimateState(AnimationState state, float rate = 1.0f, bool isCarry = false);


    /*
     * Task Coroutines
     * */

    // TODO: Combine Animate() with begin and completion delegates

    //protected virtual void BeginTaskActionDelegate() { }
    //protected virtual void CompleteTaskActionDelegate() { }

    protected Coroutine performTaskCoroutine;
    protected IEnumerator PerformTaskAction(Action<bool> callBack) {
        coroutinesCount["task"]++;

        float speed = SpeedForTask(currentMasterTask.actionType) * ResearchSingleton.sharedInstance.unitActionMultiplier;

        //BeginTaskActionDelegate();

        bool isCarrying = resourceManager.isHoldingResources(this);
        AnimateState(AnimationState.PerformCoreAction, 1.0f, isCarrying);


        while (true) {

            // Don't perform on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            if (currentGameTask.actionItem == null) {
                callBack(false);
                AnimateState(AnimationState.Idle);
                //CompleteTaskActionDelegate();
                coroutinesCount["task"]--;
                yield break;
            }

            float completion = currentGameTask.actionItem.performAction(currentGameTask, Time.deltaTime * speed, this);
            unitStatusTooltip?.percentageBar.SetPercent(completion);

            //Animate();

            if (completion >= 1) {
                callBack(true);
                AnimateState(AnimationState.Idle);
                //CompleteTaskActionDelegate();
                coroutinesCount["task"]--;

                yield break;
            }

            yield return null;
        }     
    }

    //protected virtual void BeginWalkDelegate() { }
    //protected virtual void CompleteWalkDelegate() { }

    protected Coroutine followPathCoroutine;    
    protected IEnumerator FollowPath(Path path, System.Action<bool> callBack) {
        coroutinesCount["path"]++;

        Vector3 startPoint = path.lookPoints[0].worldPosition.vector3;
        startPoint.y = transform.position.y;
  
        // If we are not moving at all (with some grey area) then don't bother turning to it.
        if(Vector3.Distance(startPoint, transform.position) > 5) {
            turnCoroutine = StartCoroutine(TurnInPlace(startPoint - transform.position));
            yield return turnCoroutine;
        }

        // Keep track of our waypoints
        int pathIndex = 0;
        Vector3 previousWaypointPosition = transform.position;    

        // Used to slow as we approach target
        float speedPercent = 1;

        // Keep track of our total journey
        currentPercentOfJournery = 0;
        float basePercentOfJourney = 0;

        // Animate differently if unit is carying something
        bool isCarrying = resourceManager.isHoldingResources(this);

        bool followingPath = true;

        // If we are only moving one space, and that space is very short, don't bother (fixes units spinning around when doing a task in same location)
        if(path.lookPoints.Length == 1 && Vector3.Distance(path.lookPoints[pathIndex].worldPosition.vector3, transform.position) < 5) {
            followingPath = false;
        }

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

                AnimateState(AnimationState.Walk, Mathf.InverseLerp(10, 100, localSpeed), isCarrying);

                float height = Script.Get<MapsManager>().GetHeightAt(lookPointMapCoordinate) * lookPointMapCoordinate.mapContainer.transform.lossyScale.y; //  + (0.5f * transform.localScale.y)
                Vector3 lookPoint = new Vector3(lookPointWorldPos.vector3.x, height, lookPointWorldPos.vector3.z);

                Quaternion targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
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

        Vector3 finalLookAtBalancedHeight = path.finalLookAt.vector3;
        finalLookAtBalancedHeight.y = transform.position.y;

        turnCoroutine = StartCoroutine(TurnInPlace(finalLookAtBalancedHeight - transform.position)); ;
        yield return turnCoroutine;

        callBack(true);
        coroutinesCount["path"]--;
    }

    protected Coroutine turnCoroutine;
    private IEnumerator TurnInPlace(Vector3 lookAt) {
        coroutinesCount["turn"]++;

        //Quaternion originalRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(lookAt);
        
        //float degreesToTurn = (targetRotation.eulerAngles - originalRotation.eulerAngles).magnitude;
        //float totalTurnDistance = 0;

        // Use the cross product to determine if we are moving left or right
        Vector3 cross = Vector3.Cross(transform.forward, lookAt);

        // Alter animation if unit is carrying something
        bool isCarrying = resourceManager.isHoldingResources(this);

        //print("degreesToTurn: " + degreesToTurn);

        while(true) {

            // Don't move on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            float degreesToTurn = (targetRotation.eulerAngles - transform.rotation.eulerAngles).magnitude;

            if(degreesToTurn > 1) {
                AnimateState(cross.y > 0 ? AnimationState.TurnRight : AnimationState.TurnLeft, Mathf.InverseLerp(-5, 5, turnSpeed), isCarrying);

                //float additionalDistance = ((Time.deltaTime * turnSpeed) / degreesToTurn * 180);

                //print("additionalDistance: " + additionalDistance);

                //totalTurnDistance = Mathf.Clamp01(totalTurnDistance + additionalDistance);
                //transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, totalTurnDistance);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 90);

            } else {
                coroutinesCount["turn"]--;
                yield break;
            }

            yield return null;
        }
    }

    /*
     * TerrainUpdateDelegate Interface
     * */

    public void NotifyTerrainUpdate(LayoutCoordinate layoutCoordinate) {
        refuseTaskSet.Clear();

        // The idea is to find a shorter path if one has become available... but I think this is messing up the associatedTask to 'unknown' objects, specifically the ore.
        // We become associated and then find a new path without unassociating

        //if (currentMasterTask != null && navigatingToTask == true && currentGameTask.target.vector3 != this.transform.position) {
        //    // Request a new path if the world has updated and we are already on the move
        //    RequestPath(transform.position, movementPenaltyMultiplier, currentGameTask, foundWaypoints);
        //}
    }


    /*
     * Actionable Item
     * */

    float attackActionPercent = 0;
    const float attackModifierSpeed = 1f;
    const float attackRangedModifierSpeed = 1f;

    public override float performAction(GameTask task, float rate, Unit unit) {
        switch(task.action) {

            case GameTask.ActionType.AttackMele:
                attackActionPercent += rate * attackModifierSpeed;

                if(attackActionPercent >= 1) {
                    attackActionPercent = 1;

                    // The associatedTask is over
                    AssociateTask(null);
                    TakeDamage(Mathf.Lerp(EnemyManager.minEnemyAttack, EnemyManager.maxEnemyAttack, EnemyManager.evolution));

                    //GameResourceManager resourceManager = Script.Get<GameResourceManager>();
                    //resourceManager.GiveToUnit(this, unit);

                    attackActionPercent = 0;

                    return 1;
                }

                return attackActionPercent;
            case GameTask.ActionType.AttackRanged:
                attackActionPercent += rate * attackRangedModifierSpeed;

                if(attackActionPercent >= 1) {
                    attackActionPercent = 1;

                    // The associatedTask is over
                    AssociateTask(null);
                    TakeDamage(10);

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

    public void TakeDamage(float damage) {
        if (remainingHealth <= 0) {
            // We have already died
            return;
        }

        remainingHealth -= Mathf.FloorToInt(damage);

        if (remainingHealth <= 0) {
            remainingHealth = 0;

            if (factionType == FactionType.Player) {
                Script.Get<NotificationPanel>().AddNotification(new NotificationItem($"{Unit_Noun} has been destroyed", NotificationType.UnitKilled, transform, primaryActionType));
            } else {
                Script.Get<NotificationPanel>().AddNotification(new NotificationItem("Enemy has been destroyed", NotificationType.EnemyKilled, transform, primaryActionType));
            }

            Script.Get<UnitManager>().DisableUnit(this);
            Shutdown();
        }

        unitStatusTooltip.SetRemainingHealth(remainingHealth, (float)remainingHealth / (float)unitHealth);
    }

    public void SetRemainingDuration(int remainingTime) {
        this.remainingDuration = remainingTime;
        float percentOfMaxUnitTime = (float)remainingTime / (float)maxUnitUduration;

        unitStatusTooltip?.SetRemainingDuration(remainingTime, percentOfMaxUnitTime);

        if(remainingTime == NotificationPanel.unitDurationWarning) {
            Script.Get<NotificationPanel>().AddNotification(new NotificationItem($"{Unit_Noun} only has " + NotificationPanel.unitDurationWarning.ToString() + "s remaining", NotificationType.Warning, transform, primaryActionType));
        }
    }

    /*
     * Selectable Interface
     * */

    public void SetSelected(bool selected) {

        Color color = Color.white;
        if(selected) {
            color = ColorSingleton.sharedInstance.highlightColor;
        }

        if (buildableComponent != null) {
            foreach(Building.MeshBuildingTier meshTier in buildableComponent.meshTiers) {
                foreach(Renderer renderer in meshTier.meshRenderes) {
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

    public override void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Add(notificationDelegate);

        // Let the subscriber know our status immediately
        notificationDelegate.NowPerformingTask(this, currentMasterTask, currentGameTask);
    }

    public override void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        taskStatusDelegateList.Remove(notificationDelegate);
    }

    protected override void NotifyAllTaskStatus() {
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

        public static Blueprint Miner = new Blueprint("Miner", typeof(Miner), null, MasterGameTask.ActionType.Mine, "Miner", $"Basic Mining {Unit_Noun}",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 },
                { MineralType.Silver, 1 }                
            }));

        public static Blueprint Mover = new Blueprint("Mover", typeof(Mover), null, MasterGameTask.ActionType.Move, "Mover", $"Basic Moving {Unit_Noun}",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 }                                
            }));

        public static Blueprint Builder = new Blueprint("Builder", typeof(Builder), null, MasterGameTask.ActionType.Build, "Builder", $"Basic Building {Unit_Noun}",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 },
                { MineralType.Silver, 2 }                
            }));


        public static Blueprint Defender = new Blueprint("Defender", typeof(Defender), null, MasterGameTask.ActionType.Attack, "Defender", "Hunts Rock Golems",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 1 },
                { MineralType.Silver, 3 }
            }));

        public static Blueprint AdvancedMiner = new Blueprint("AdvancedMiner", typeof(AdvancedMiner), null, MasterGameTask.ActionType.Mine, "Adv. Miner", "High Mining speed", 
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 3 },
                { MineralType.Gold, 2 }
            }),
            (LayoutCoordinate layoutCoordinate) => {
                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(AdvUnitBuilding));                
            },
            "Build Adjacent to " + Building.Blueprint.AdvUnitBuilding.label            
            );

        public static Blueprint AdvancedMover = new Blueprint("AdvancedMover", typeof(AdvancedMover), null, MasterGameTask.ActionType.Move, "Adv. Mover", "Terrain has no effect on this Unit",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 4}                
            }),
            (LayoutCoordinate layoutCoordinate) => {
                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(AdvUnitBuilding));
            },
            "Build Adjacent to " + Building.Blueprint.AdvUnitBuilding.label
            );

        public static Blueprint AdvancedBuilder = new Blueprint("AdvancedBuilder", typeof(AdvancedBuilder), null, MasterGameTask.ActionType.Build, "Adv. Builder", "High Building speed",
            new BlueprintCost(new Dictionary<MineralType, int>(){                
                { MineralType.Silver, 3 },
                { MineralType.Gold, 2 }
            }),
            (LayoutCoordinate layoutCoordinate) => {
                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(AdvUnitBuilding));
            },
            "Build Adjacent to " + Building.Blueprint.AdvUnitBuilding.label
            );

        public Blueprint(string fileName, Type type, string iconName, MasterGameTask.ActionType? actionType, string label, string description, BlueprintCost cost) : base(folder + fileName, type, iconName, actionType, label, description, cost) { }

        public Blueprint(string fileName, Type type, string iconName, MasterGameTask.ActionType? actionType, string label, string description, BlueprintCost cost, Func<LayoutCoordinate, bool> requirementsMet, string notMetString) : 
            base(folder + fileName, type, iconName, actionType, label, description, cost, requirementsMet, notMetString) { }


        public override GameObject ConstructAt(LayoutCoordinate layoutCoordinate) {
            UnitManager unitManager = Script.Get<UnitManager>();
            Unit unit = UnityEngine.Object.Instantiate(resource) as Unit;

            UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();

            unitManager.BuildAt(unitBuilding, layoutCoordinate, cost);

            return unit.gameObject;
        }
    }
}
