﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TaskStatusNotifiable {
    void RegisterForNotifications(TaskStatusUpdateDelegate notificationDelegate);
    void EndNotifications(TaskStatusUpdateDelegate notificationDelegate);
}

public interface TaskStatusUpdateDelegate {
    void NowPerformingTask(MasterGameTask masterGameTask, GameTask gameTask);
}

public abstract class Unit : MonoBehaviour, Selectable, TerrainUpdateDelegate, TaskStatusNotifiable
{
    public float speed;
    public float turnSpeed;
    public float turnDistance;
    public float stoppingDistance;

    UnitStatusTooltip unitStatusPanel;

    Path path;

    bool navigatingToTask;
    TaskQueueManager taskQueueManager;


    
    MasterGameTask currentMasterTask;
    GameTask currentGameTask;
    Queue<GameTask> gameTasksQueue;

    //Map map;

    public Transform statusLocation;

    public static int unitCount = 0;

    abstract public MasterGameTask.ActionType primaryActionType { get; }

    abstract public float SpeedForTask(GameTask task);

    // Selectable Interface
    private string title;
    public string description => title;

    public List<TaskStatusUpdateDelegate> delegateList = new List<TaskStatusUpdateDelegate>();

    //public StatusDelegate statusDelegate;

    private void Start() {
        unitStatusPanel = Instantiate(Resources.Load("UnitStatusPanel", typeof(UnitStatusTooltip))) as UnitStatusTooltip;
        unitStatusPanel.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>());

        unitStatusPanel.SetFollower(statusLocation);

        unitStatusPanel.SetTitle(title);
        unitStatusPanel.SetTask(null);
        unitStatusPanel.DisplayPercentageBar(false);

        //map = Script.Get<MapContainer>().getMap();
    }

    private void Awake() { 

        title = "Unit #" + unitCount;
        unitCount++;

        gameTasksQueue = new Queue<GameTask>();

        // PATH FLOW INIT

        // DO PATH FLOW

        completedTaskAction = (pathComplete) => {
            unitStatusPanel.DisplayPercentageBar(false);
            ContinueGameTaskQueue();
        };

        completedPath = (pathComplete) => {
            print("Do Action for Task" + currentMasterTask.taskNumber);
            navigatingToTask = false;
            unitStatusPanel.DisplayPercentageBar(true);
            StartCoroutine(PerformTaskAction(completedTaskAction));
        };

        foundWaypoints = (waypoints, actionableItem, success) => {
            StopAllCoroutines();

            if(success) {
                print("Follow Path for Task" + currentMasterTask.taskNumber);

                path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);

                // When requesting a path for an unknown resource (like ore) we will get the closest resource back as an actionable item
                if (actionableItem != null) {
                    currentGameTask.actionItem = actionableItem;
                    actionableItem.AssociateTask(currentGameTask);
                }                

                StartCoroutine(FollowPath(completedPath));
            } else {
                // There is no path to task, we cannot do this.
                print("Give up Task" + currentMasterTask.taskNumber);
                print("Put task back in Queue");

                taskQueueManager.QueueTask(currentMasterTask);
                timeBeforeNextTaskCheck = 1;

                navigatingToTask = false;

                gameTasksQueue.Clear();
                currentGameTask = null;
                currentMasterTask = null;
            }
        };
    }

    public void Init() {
        taskQueueManager = Script.Get<TaskQueueManager>();
        Script.Get<MapsManager>().AddTerrainUpdateDelegate(this);
    }

    float timeBeforeNextTaskCheck = 0;
    private void Update() {

        return;

        // Check Task
        if(timeBeforeNextTaskCheck <= 0) {
            timeBeforeNextTaskCheck = 0.10f;

            if(currentMasterTask == null) {
                currentMasterTask = taskQueueManager.GetNextDoableTask(this);

                // if we still have to task to do, we are IDLE
                if (currentMasterTask == null) {
                    return;
                }

                print("Start Task " + currentMasterTask.taskNumber);
                DoTask();                
            }
        }

        timeBeforeNextTaskCheck -= Time.deltaTime;
    }

    private void OnDestroy() {
        Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
    }

    // DO PATH FLOW

    System.Action<bool> completedTaskAction;

    System.Action<bool> completedPath;

    System.Action<WorldPosition[], ActionableItem, bool> foundWaypoints;

    private void DoTask() {

        unitStatusPanel.SetTask(currentMasterTask);

        gameTasksQueue.Clear();

        //navigatingToTask = true;
        foreach (GameTask task in currentMasterTask.childGameTasks) {
            gameTasksQueue.Enqueue(task);
        }

        ContinueGameTaskQueue();
    }

    private void ContinueGameTaskQueue() {

        if (gameTasksQueue.Count > 0) {
            currentGameTask = gameTasksQueue.Dequeue();

            // Let the UI Know our status is changing
            NotifyAllTaskStatus();

            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        } else {
            print("Complpete Task" + currentMasterTask.taskNumber);

            currentMasterTask.MarkTaskFinished();
            currentMasterTask = null;
            currentGameTask = null;
            timeBeforeNextTaskCheck = 0;
            unitStatusPanel.SetTask(currentMasterTask);
        }
    }

    IEnumerator PerformTaskAction(System.Action<bool> callBack) {

        float speed = SpeedForTask(currentGameTask);

        while (true) {
            float completion = currentGameTask.actionItem.performAction(currentGameTask, Time.deltaTime * speed, this);
            unitStatusPanel.percentageBar.SetPercent(completion);

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

    // Selectable Interface
    public void SetSelected(bool selected) {

        Color tintColor = selected ? Color.cyan : Color.white;
        gameObject.GetComponent<MeshRenderer>().material.color = tintColor;
    }

    //public void SetStatusDelegate(StatusDelegate statusDelegate) {
    //    this.statusDelegate = statusDelegate;

    //    if (statusDelegate != null) {
    //        statusDelegate.InformCurrentTask(masterTask, currentGameTask);
    //    }
    //}

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
        }
    }

    // Terrain Update Delegate Interface

    public void NotifyTerrainUpdate() {
        if (currentMasterTask != null && navigatingToTask == true && currentGameTask.target.vector3 != this.transform.position) {
            // Request a new path if the world has updated and we are already on the move
            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        }
    }

    /*
     * TaskStatusNotifiable Interface
     * */

    public void RegisterForNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        delegateList.Add(notificationDelegate);

        // Let the subscriber know our status immediately
        notificationDelegate.NowPerformingTask(currentMasterTask, currentGameTask);
    }

    public void EndNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        delegateList.Remove(notificationDelegate);
    }

    public void NotifyAllTaskStatus() {
        foreach(TaskStatusUpdateDelegate updateDelegate in delegateList) {
            updateDelegate.NowPerformingTask(currentMasterTask, currentGameTask);
        }
    }

}