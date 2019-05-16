using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Line
{
    const float verticalLineGradient = 1e5f;

    float gradient;
    float y_intercept;

    Vector2 pointOnLine_1;
    Vector2 pointOnLine_2;

    float gradientPerpendicular;

    bool approachSide;

    public Line(Vector2 pointOnLine, Vector2 PointPerpendicularToLine) {
        float dx = pointOnLine.x - PointPerpendicularToLine.x;
        float dy = pointOnLine.y - PointPerpendicularToLine.y;

        if (dx == 0) {
            gradientPerpendicular = verticalLineGradient;
        } else {
            gradientPerpendicular = dy / dx;
        }

        // Gradient line * gradient of perpendicular line = -1 
        if (gradientPerpendicular == 0) {
            gradient = verticalLineGradient;
        } else {
            gradient = -1 / gradientPerpendicular;
        }

        y_intercept = pointOnLine.y - gradient * pointOnLine.x;

        pointOnLine_1 = pointOnLine;
        pointOnLine_2 = pointOnLine + new Vector2(1, gradient);

        approachSide = false;
        approachSide = GetSide(PointPerpendicularToLine);
    }

    bool GetSide(Vector2 p) {
        return (p.x - pointOnLine_1.x) * (pointOnLine_2.y - pointOnLine_1.y) > (p.y - pointOnLine_1.y) * (pointOnLine_2.x - pointOnLine_1.x);
    }

    public bool HasCrossedLine(Vector2 p) {
        return GetSide(p) != approachSide;
    }

    public float DistanceFromPoint(Vector2 p) {
        // Find the y intercept of the line through point p
        // The new perpendicular line created from point p will have this Y intercept.

        // c = y - mx

        float yInterceptPerpendicular = p.y - gradientPerpendicular * p.x;
        
        // Our original line has y = mx + c
        // The two lines will cross where x and y are equal
        // ->> m1x + c1 = m2x + c2
        // ->> x = (c2 - c1) / (m1 - m2)


        float intersectX = (yInterceptPerpendicular - y_intercept) / (gradient - gradientPerpendicular);
        float intersectY = gradient * intersectX + y_intercept;

        return Vector2.Distance(p, new Vector2(intersectX, intersectY));
    }

    public void DrawWithGizmos(float length) {
        Vector3 lineDir = new Vector3(1, 0, gradient).normalized;

        // 3d version of point on line1
        Vector3 lineCenter = new Vector3(pointOnLine_1.x, 0, pointOnLine_1.y) + Vector3.up * 10;


        Gizmos.DrawLine(lineCenter - lineDir * length / 2f, lineCenter + lineDir * length / 2f);
    }
}
