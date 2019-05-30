using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    //public Transform target;

    public float speed;
    public float turnSpeed;
    public float turnDistance;
    public float stoppingDistance;

    Path path;

    GameTask task;
    TaskQueue taskQueue;

    bool performingTask = false;

    private void Start() {
        taskQueue = Script.Get<TaskQueue>();
    }

    private void Update() {

        if (task == null) {
            if (taskQueue.Count() == 0) {
                return;
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

    private void DoTask() {
        //StopCoroutine(SearchForTask(Script.Get<TaskQueue>()));
        //StopCoroutine(FollowPath());

        System.Action<bool> completedTaskAction = (pathComplete) => {
            print("Complpete Task" + task.taskNumber);

            performingTask = false;
            task = null;
        };

        System.Action<bool> completedPath = (pathComplete) => {
            print("Do Action for Task" + task.taskNumber);
            StartCoroutine(PerformTaskAction(completedTaskAction));
        };

        System.Action<WorldPosition[], bool> foundWaypoints = (waypoints, success) => {
            if(success) {
                print("Follow Path for Task" + task.taskNumber);


                path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);



                StartCoroutine(FollowPath(completedPath));
            } else {
                // There is no path to task, we cannot do this.
                print("Give up Task" + task.taskNumber);

                performingTask = false;
                task = null;
            }
        };

        PathRequestManager.RequestPath(transform.position, task.target.vector3, foundWaypoints);
    }

    private void WaypointsFound(Vector3[] waypoints, bool success) { }

    //public void beginPathFinding() {
    //    PathRequestManager.RequestPath(transform.position, target.position, (waypoints, success) => {
    //        if(success) {
    //            path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);
    //            StopCoroutine(FollowPath());
    //            StartCoroutine(FollowPath());
    //        }
    //    });
    //}

    //IEnumerator SearchForTask(TaskQueue taskQueue) {
    //    while (true) {
    //        if(taskQueue.Count() > 0) {
    //            task = taskQueue.Pop();
    //            DoTask();
    //        }

    //        // If nothing to queue
    //        yield return new WaitForSeconds(0.5f);
    //    }        
    //}

    //int pathIndex = 0;
    /*private void FollowPath() {

        //bool followingPath = true;

        //transform.LookAt(path.lookPoints[0].vector3);


        //while(followingPath) {
            Vector2 position2D = transform.position.ToVector2();
            while(path.turnBoundaries[pathIndex].HasCrossedLine(position2D)) {
                if(pathIndex == path.finishLineIndex) {
                    //followingPath = false;
                    //BeginQueueing();
                    return;
                } else {
                    pathIndex++;
                }
            }

        //if(followingPath) {
        float speedPercent = 1;

        if(pathIndex >= path.slowDownIndex && stoppingDistance > 0) {
            speedPercent = Mathf.Clamp(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(position2D) / stoppingDistance, 0.15f, 1);
        }

        Vector3 lookPoint = path.lookPoints[pathIndex].vector3;
        lookPoint.y = transform.position.y;
        Quaternion targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
            //}

            //yield return null;
        //}
    }*/

    IEnumerator PerformTaskAction(System.Action<bool> callBack) {

        float speed = 1f;
        ////bool performingAction = true;

        while (true) {
            float completion = task.actionItem.performAction(task.action, Time.deltaTime * speed);

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

    public void SetSelected(bool selected) {

        Color tintColor = selected ? Color.cyan : Color.white;
        gameObject.GetComponent<MeshRenderer>().material.color = tintColor;
    }

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
        }
    }
}
