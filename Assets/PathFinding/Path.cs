using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions {
    public static Vector3 ToVector2(this Vector3 v3) {
        return new Vector2(v3.x, v3.z);
    }
}

public class LookPoint {
    public WorldPosition worldPosition;
    public int movementPoints;
    public float percentOfJourney;

    public LookPoint(WorldPosition worldPosition, int movementPoints) {
        this.worldPosition = worldPosition;
        this.movementPoints = movementPoints;
    }
}

public class Path {
    public readonly LookPoint[] lookPoints;
    public readonly Line[] turnBoundaries;
    public readonly int finishLineIndex;
    public readonly int slowDownIndex;
    public readonly WorldPosition finalLookAt;

    public Path(LookPoint[] lookPoints, Vector3 startPos, float turnDistance, float stoppingDistance, WorldPosition finalLookAt) {
        this.lookPoints = lookPoints;
        turnBoundaries = new Line[lookPoints.Length];
        finishLineIndex = turnBoundaries.Length - 1;
        this.finalLookAt = finalLookAt;

        Vector2 previousPoint = startPos.ToVector2();
        for (int i = 0; i < lookPoints.Length; i++) {
            Vector2 currentPoint = lookPoints[i].worldPosition.vector3.ToVector2();
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
            distanceFromEndpoint += Vector3.Distance(lookPoints[i].worldPosition.vector3, lookPoints[i - 1].worldPosition.vector3);

            if (distanceFromEndpoint > stoppingDistance) {
                slowDownIndex = i;
                break;
            }
        }
    }
    
    public void DrawWithGizmos() {
        foreach (LookPoint lp in lookPoints) {
            Gizmos.DrawCube(lp.worldPosition.vector3 + Vector3.up * 100, Vector3.one);        
        }

        Gizmos.color = Color.white;
        foreach (Line l in turnBoundaries) {
            l.DrawWithGizmos(100);
        }
    }

}
