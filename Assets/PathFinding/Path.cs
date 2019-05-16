using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions {
    public static Vector3 ToVector2(this Vector3 v3) {
        return new Vector2(v3.x, v3.z);
    }
}

public class Path
{
    public readonly Vector3[] lookPoints;
    public readonly Line[] turnBoundaries;
    public readonly int finishLineIndex;
    public readonly int slowDownIndex;

    public Path(Vector3[] waypoints, Vector3 startPos, float turnDistance, float stoppingDistance) {
        lookPoints = waypoints;
        turnBoundaries = new Line[lookPoints.Length];
        finishLineIndex = turnBoundaries.Length - 1;

        Vector2 previousPoint = startPos.ToVector2();
        for (int i = 0; i < lookPoints.Length; i++) {
            Vector2 currentPoint = lookPoints[i].ToVector2();
            Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
            Vector2 turnBoundaryPoint = currentPoint - dirToCurrentPoint * turnDistance;

            // Our last turn boundary is the point itself
            if (i == finishLineIndex) {
                turnBoundaryPoint = currentPoint;
            }

            turnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDistance);
            previousPoint = turnBoundaryPoint;
        }

        float distanceFromEndpoint = 0;

        for(int i = lookPoints.Length - 1; i > 0; i--) {
            distanceFromEndpoint += Vector3.Distance(lookPoints[i], lookPoints[i - 1]);

            if (distanceFromEndpoint > stoppingDistance) {
                slowDownIndex = i;
                break;
            }
        }
    }
    
    public void DrawWithGizmos() {

        MonoBehaviour.print("Count: " + lookPoints.Length);
        foreach (Vector3 p in lookPoints) {
            Gizmos.DrawCube(p + Vector3.up * 10, Vector3.one);        
        }

        Gizmos.color = Color.white;
        foreach (Line l in turnBoundaries) {
            l.DrawWithGizmos(10);
        }
    }

}
