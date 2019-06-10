using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour, Selectable, TerrainUpdateDelegate
{
    public float speed;
    public float turnSpeed;
    public float turnDistance;
    public float stoppingDistance;

    UnitStatusPanel unitStatusPanel;

    Path path;

    MasterGameTask masterTask;
    bool navigatingToTask;
    TaskQueueManager taskQueueManager;

    GameTask currentGameTask;
    Queue<GameTask> gameTasksQueue;

    public Transform statusLocation;

    public static int unitCount = 0;

    abstract public MasterGameTask.ActionType primaryActionType { get; }

    // Selectable Interface
    private string title;
    public string description => title;
    public StatusDelegate statusDelegate;

    private void Start() {
        unitStatusPanel = Instantiate(Resources.Load("UnitStatusPanel", typeof(UnitStatusPanel))) as UnitStatusPanel;
        unitStatusPanel.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>());

        unitStatusPanel.SetFollower(statusLocation);

        unitStatusPanel.SetTitle(title);
        unitStatusPanel.SetTask(null);
        unitStatusPanel.DisplayPercentageBar(false);
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
            print("Do Action for Task" + masterTask.taskNumber);
            navigatingToTask = false;
            unitStatusPanel.DisplayPercentageBar(true);
            StartCoroutine(PerformTaskAction(completedTaskAction));
        };

        foundWaypoints = (waypoints, actionableItem, success) => {
            StopAllCoroutines();

            if(success) {
                print("Follow Path for Task" + masterTask.taskNumber);

                path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);

                // When requesting a path for an unknown resource (like ore) we will get the closest resource back as an actionable item
                if (actionableItem != null) {
                    currentGameTask.actionItem = actionableItem;
                    actionableItem.AssociateTask(currentGameTask);
                }                

                StartCoroutine(FollowPath(completedPath));
            } else {
                // There is no path to task, we cannot do this.
                print("Give up Task" + masterTask.taskNumber);
                print("Put task back in Queue");

                taskQueueManager.QueueTask(masterTask);
                timeBeforeNextTaskCheck = 1;

                navigatingToTask = false;

                gameTasksQueue.Clear();
                currentGameTask = null;
                masterTask = null;
            }
        };
    }

    public void Init() {
        taskQueueManager = Script.Get<TaskQueueManager>();
        Script.Get<MapContainer>().getMap().AddTerrainUpdateDelegate(this);
    }

    float timeBeforeNextTaskCheck = 0;
    private void Update() {

        // Update Status

        //// Translate the world position into viewport space.
        //Vector3 screenPoint = Camera.main.WorldToScreenPoint(statusLocation.position);

        //// Used to scale up UI
        //float sizeOnScreen = 10;

        //// Get distance from screen to modify local scale as the camera moves away
        //Vector3 b = new Vector3(screenPoint.x, screenPoint.y + sizeOnScreen, screenPoint.z);

        //Vector3 aa = Camera.main.ScreenToWorldPoint(screenPoint);
        //Vector3 bb = Camera.main.ScreenToWorldPoint(b);

        //unitStatusPanel.transform.localScale = Vector3.one * (1.0f / (aa - bb).magnitude);

        //// Canvas local coordinates are relative to its center, 
        //// so we offset by half. We also discard the depth.
        //screenPoint -= 0.5f * Vector3.one;
        //screenPoint.z = 0;

        //unitStatusPanel.transform.position = screenPoint;


        // Check Task
        if(timeBeforeNextTaskCheck <= 0) {
            timeBeforeNextTaskCheck = 0.10f;

            if(masterTask == null) {
                masterTask = taskQueueManager.GetNextDoableTask(this);

                // if we still have to task to do, we are IDLE
                if (masterTask == null) {
                    return;
                }

                print("Start Task " + masterTask.taskNumber);
                DoTask();                
            }
        }

        timeBeforeNextTaskCheck -= Time.deltaTime;
    }

    private void OnDestroy() {
        Script.Get<MapContainer>().getMap().RemoveTerrainUpdateDelegate(this);
    }

    // DO PATH FLOW

    System.Action<bool> completedTaskAction;

    System.Action<bool> completedPath;

    System.Action<WorldPosition[], ActionableItem, bool> foundWaypoints;

    private void DoTask() {

        unitStatusPanel.SetTask(masterTask);

        // Let the UI Know our status is changing
        if(statusDelegate != null) {
            statusDelegate.InformCurrentTask(masterTask);
        }

        gameTasksQueue.Clear();

        //navigatingToTask = true;
        foreach (GameTask task in masterTask.childGameTasks) {
            gameTasksQueue.Enqueue(task);
        }

        ContinueGameTaskQueue();
    }

    private void ContinueGameTaskQueue() {

        if (gameTasksQueue.Count > 0) {
            currentGameTask = gameTasksQueue.Dequeue();

            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        } else {
            print("Complpete Task" + masterTask.taskNumber);

            masterTask.MarkTaskFinished();
            masterTask = null;
            currentGameTask = null;
            timeBeforeNextTaskCheck = 0;
            unitStatusPanel.SetTask(masterTask);
        }
    }

    IEnumerator PerformTaskAction(System.Action<bool> callBack) {

        float speed = 0.25f;

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

        transform.LookAt(path.lookPoints[0].vector3);

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

                Vector3 lookPoint = path.lookPoints[pathIndex].vector3;
                lookPoint.y = transform.position.y;
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

    public void SetStatusDelegate(StatusDelegate statusDelegate) {
        this.statusDelegate = statusDelegate;

        if (statusDelegate != null) {
            statusDelegate.InformCurrentTask(masterTask);
        }
    }

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
        }
    }

    // Terrain Update Delegate Interface

    public void NotifyTerrainUpdate() {
        if (masterTask != null && navigatingToTask == true && currentGameTask.target.vector3 != this.transform.position) {
            // Request a new path if the world has updated and we are already on the move
            PathRequestManager.RequestPathForTask(transform.position, currentGameTask, foundWaypoints);
        }
    }
}
