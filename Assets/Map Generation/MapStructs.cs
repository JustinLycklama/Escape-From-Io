using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LayoutCoordinate {
    public int x;
    public int y;

    public LayoutCoordinate(MapCoordinate coodrinate) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        x = (int)coodrinate.x / constants.featuresPerLayoutPerAxis;
        y = (int)coodrinate.y / constants.featuresPerLayoutPerAxis;
    }

    public LayoutCoordinate(int layoutX, int layoutY) {
        x = layoutX;
        y = layoutY;
    }

    public override bool Equals(object obj) {
        if(!(obj is LayoutCoordinate)) {
            return false;
        }

        var point = (LayoutCoordinate)obj;
        return x == point.x && y == point.y;
    }

    public override int GetHashCode() {
        var hashCode = 499445682;
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(LayoutCoordinate a, LayoutCoordinate b) {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(LayoutCoordinate a, LayoutCoordinate b) {
        return !(a == b);
    }
}

public struct PathGridCoordinate {
    public float x;
    public float y;

    public int xLowSample { get { return Mathf.FloorToInt(x); } }
    public int xHighSample { get {
            Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();
            return Mathf.Clamp(xLowSample, 0, constants.layoutMapWidth * constants.nodesPerLayoutPerAxis - 1);
        } }

    public int yLowSample { get { return Mathf.FloorToInt(y); } }
    public int yHighSample { get {
            Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();
            return Mathf.Clamp(yLowSample, 0, constants.layoutMapHeight * constants.nodesPerLayoutPerAxis - 1);
        } }

    public PathGridCoordinate(MapCoordinate mapCoordinate) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        x = mapCoordinate.x / constants.featuresPerLayoutPerAxis * constants.nodesPerLayoutPerAxis;
        y = mapCoordinate.y / constants.featuresPerLayoutPerAxis * constants.nodesPerLayoutPerAxis;
    }

    public PathGridCoordinate(float x, float y) {
        this.x = x;
        this.y = y;
    }
}

public struct MapCoordinate {
    public float x;
    public float y;

    public int xLowSample { get { return Mathf.FloorToInt(x); } }
    public int xHighSample { get { return Mathf.Clamp(xLowSample, 0, Tag.Narrator.GetGameObject().GetComponent<Constants>().mapWidth); } }

    public int yLowSample { get { return Mathf.FloorToInt(y); } }
    public int yHighSample { get { return Mathf.Clamp(yLowSample, 0, Tag.Narrator.GetGameObject().GetComponent<Constants>().mapHeight); } }

    public MapCoordinate(PathGridCoordinate pathGridCoordinate) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        x = pathGridCoordinate.x / constants.nodesPerLayoutPerAxis * constants.featuresPerLayoutPerAxis;
        y = pathGridCoordinate.y / constants.nodesPerLayoutPerAxis * constants.featuresPerLayoutPerAxis;
    }

    public MapCoordinate(WorldPosition worldPosition) {

        GameObject map = Tag.Map.GetGameObject();
        Transform mapObjectSpace = map.transform;
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        Vector3 mapPosition = mapObjectSpace.InverseTransformPoint(worldPosition.vector3);

        x = mapPosition.x + constants.mapWidth / 2f;
        y = - mapPosition.z + constants.mapHeight / 2f;
    }
    
    public MapCoordinate(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public override bool Equals(object obj) {
        if(!(obj is MapCoordinate)) {
            return false;
        }

        var coordinate = (MapCoordinate)obj;
        return x == coordinate.x &&
               y == coordinate.y;
    }

    public override int GetHashCode() {
        var hashCode = 1502939027;
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(LayoutCoordinate a, MapCoordinate coordinate) {
        LayoutCoordinate b = new LayoutCoordinate(coordinate);

        return a.x == b.x && a.y == b.y;
    }

    public static bool operator ==(MapCoordinate coordinate, LayoutCoordinate b) {
        LayoutCoordinate a = new LayoutCoordinate(coordinate);

        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(LayoutCoordinate a, MapCoordinate b) {
        return !(a == b);
    }

    public static bool operator !=(MapCoordinate a, LayoutCoordinate b) {
        return !(a == b);
    }
}

public struct WorldPosition {
    public float x;
    public float y;
    public float z;

    public Vector3 vector3 {
        get { return new Vector3(x, y, z); }
    }

    public MapCoordinate mapCoordinate {
        get { return new MapCoordinate(this); }
    }

    public WorldPosition(Vector3 position) {
        x = position.x;
        y = position.y;
        z = position.z;
    }    

    public WorldPosition(MapCoordinate mapCoordinate) {
        GameObject mapObject = Tag.Map.GetGameObject();
        Map map = mapObject.GetComponent<MapContainer>().getMap();
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        Transform mapObjectSpace = mapObject.transform;

        Vector3 currentPointObjectSpace = new Vector3(mapCoordinate.x, map.getHeightAt(mapCoordinate), mapCoordinate.y);

        currentPointObjectSpace.x -= (constants.mapWidth / 2f);
        currentPointObjectSpace.z *= -1;
        currentPointObjectSpace.z += (constants.mapHeight / 2f);

        Vector3 positionWorldSpace = mapObjectSpace.TransformPoint(currentPointObjectSpace);

        x = positionWorldSpace.x;
        y = positionWorldSpace.y;
        z = positionWorldSpace.z;
    }

    public void recalculateHeight() {
        GameObject mapObject = Tag.Map.GetGameObject();
        Map map = mapObject.GetComponent<MapContainer>().getMap();

        y = map.getHeightAt(new MapCoordinate(this));
    }
}