using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Transform target;
    public float speed = 20;
    public float turnSpeed = 3;
    public float turnDistance = 5;
    public float stoppingDistance = 10;

    //Vector3[] path;
    //int targetIndex;

    Path path;

    private void Start() {
        PathRequestManager.RequestPath(transform.position, target.position, (waypoints, success) => {
            if (success) {
                path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);
                StopCoroutine(FollowPath());
                StartCoroutine(FollowPath());
            }
        });
    }

    IEnumerator FollowPath() {

        bool followingPath = true;
        int pathIndex = 0;

        transform.LookAt(path.lookPoints[0]);

        float speedPercent = 1;

        while(followingPath) {
            Vector2 position2D = transform.position.ToVector2();
            while(path.turnBoundaries[pathIndex].HasCrossedLine(position2D)) {
                if (pathIndex == path.finishLineIndex) {
                    followingPath = false;
                    break;
                } else {
                    pathIndex++;
                }
            }

            if (followingPath) {
                if (pathIndex >= path.slowDownIndex && stoppingDistance > 0) {
                    speedPercent = Mathf.Clamp(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(position2D) / stoppingDistance, 0.15f, 1);
                }                

                Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
            }

            yield return null;
        }


            // Old


            //Vector3 currentWayPoint = path[0];

            //while(true) {
            //    if (transform.position == currentWayPoint) {
            //        targetIndex++;
            //        if (targetIndex >= path.Length) {
            //            yield break;
            //        }
            //        currentWayPoint = path[targetIndex];
            //    }

            //    transform.position = Vector3.MoveTowards(transform.position, currentWayPoint, speed * Time.deltaTime);
            //    yield return null;
            //}
        }

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
            print("DrawPath");
        }
    }
}
