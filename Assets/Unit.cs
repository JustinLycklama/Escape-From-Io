using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, Selectable, TerrainUpdateDelegate
{
    //public Transform target;

    public float speed;
    public float turnSpeed;
    public float turnDistance;
    public float stoppingDistance;

    Path path;

    GameTask task;
    bool navigatingToTask;
    TaskQueue taskQueue;



    public static int unitCount = 0;

    string title;

    // Selectable Interface
    public string description => title;
    public StatusDelegate statusDelegate;

    private void Awake() {
        title = "Unit #" + unitCount;
        unitCount++;

        // PATH FLOW INIT

        // DO PATH FLOW

        completedTaskAction = (pathComplete) => {
            print("Complpete Task" + task.taskNumber);

            task = null;
        };

        completedPath = (pathComplete) => {
            print("Do Action for Task" + task.taskNumber);
            navigatingToTask = false;
            StartCoroutine(PerformTaskAction(completedTaskAction));
        };

        foundWaypoints = (waypoints, success) => {
            StopAllCoroutines();

            if(success) {
                print("Follow Path for Task" + task.taskNumber);

                path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);

                StartCoroutine(FollowPath(completedPath));
            } else {
                // There is no path to task, we cannot do this.
                print("Give up Task" + task.taskNumber);
                navigatingToTask = false;
                task = null;
            }
        };


    }

    private void Start() {
        taskQueue = Script.Get<TaskQueue>();
        Script.Get<MapContainer>().getMap().AddTerrainUpdateDelegate(this);
    }

    private void Update() {

        if (task == null) {
            if (taskQueue.Count() == 0) {
                // Idle
            } else {
                task = taskQueue.Pop();
                print("Start Task " + task.taskNumber);
                //pathIndex = 0;

                DoTask();
            }
        }

        //// Walk to Target
        //if (transform.position != task.target.vector3) {
        //    FollowPath();
        //} else {
        //    // Do Task action

        //    task = null;
        //}
    }

    //public void BeginQueueing() {
    //    StartCoroutine(SearchForTask(Script.Get<TaskQueue>()));


    //}

    string currentAction = "idle";

    private void OnDestroy() {
        Script.Get<MapContainer>().getMap().RemoveTerrainUpdateDelegate(this);
    }

    // DO PATH FLOW

    System.Action<bool> completedTaskAction;

    System.Action<bool> completedPath;

    System.Action<WorldPosition[], bool> foundWaypoints;

    private void DoTask() {

        if(statusDelegate != null) {
            statusDelegate.InformCurrentTask(task);
        }

        navigatingToTask = true;

        PathRequestManager.RequestPathForTask(transform.position, task, foundWaypoints);        
    }

    IEnumerator PerformTaskAction(System.Action<bool> callBack) {

        float speed = 1f;
        ////bool performingAction = true;

        while (true) {
            float completion = task.actionItem.performAction(task, Time.deltaTime * speed);

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
                    //BeginQueueing();
                    yield break; // Breaking alone does not stop a coroutine
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
            statusDelegate.InformCurrentTask(task);
        }
    }

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
        }
    }

    // Terrain Update Delegate Interface

    public void NotifyTerrainUpdate() {
        if (task != null && navigatingToTask == true && task.target.vector3 != this.transform.position) {
            // Request a new path if the world has updated and we are already on the move
            PathRequestManager.RequestPathForTask(transform.position, task, foundWaypoints);
        }
    }
}
