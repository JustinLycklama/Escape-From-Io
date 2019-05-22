using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MapPoint {
    public int virtualX;
    public int virtualY;

    public float worldX;
    public float worldY;

    public MapPoint(WorldCoordinate coodrinate) : this(coodrinate.x, coodrinate.y) {
    }

    public MapPoint(float worldX, float worldY) {
        this.worldX = worldX;
        this.worldY = worldY;

        virtualX = (int)worldX / MapGenerator.featuresPerLayoutPerAxis;
        virtualY = (int)worldY / MapGenerator.featuresPerLayoutPerAxis;
    }

    public override bool Equals(object obj) {
        if(!(obj is MapPoint)) {
            return false;
        }

        var point = (MapPoint)obj;
        return virtualX == point.virtualX &&
               virtualY == point.virtualY &&
               worldX == point.worldX &&
               worldY == point.worldY;
    }

    public override int GetHashCode() {
        var hashCode = 499445682;
        hashCode = hashCode * -1521134295 + worldX.GetHashCode();
        hashCode = hashCode * -1521134295 + worldY.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(MapPoint a, MapPoint b) {
        return a.virtualX == b.virtualX && a.virtualY == b.virtualY;
    }

    public static bool operator !=(MapPoint a, MapPoint b) {
        return !(a == b);
    }
}

public struct WorldCoordinate {
    public float x;
    public float y;

    public WorldCoordinate(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public override bool Equals(object obj) {
        if(!(obj is WorldCoordinate)) {
            return false;
        }

        var coordinate = (WorldCoordinate)obj;
        return x == coordinate.x &&
               y == coordinate.y;
    }

    public override int GetHashCode() {
        var hashCode = 1502939027;
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(MapPoint a, WorldCoordinate coordinate) {
        MapPoint b = new MapPoint(coordinate);

        return a.virtualX == b.virtualX && a.virtualY == b.virtualY;
    }

    public static bool operator ==(WorldCoordinate coordinate, MapPoint b) {
        MapPoint a = new MapPoint(coordinate);

        return a.virtualX == b.virtualX && a.virtualY == b.virtualY;
    }

    public static bool operator !=(MapPoint a, WorldCoordinate b) {
        return !(a == b);
    }

    public static bool operator !=(WorldCoordinate a, MapPoint b) {
        return !(a == b);
    }
}