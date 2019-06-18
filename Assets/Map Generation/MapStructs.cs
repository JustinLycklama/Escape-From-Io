﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LayoutCoordinate {
    public int x;
    public int y;

    public string description {
        get {
            return "x: " + x + " y: " + y + ", of map X " + mapContainer.mapX + " Y " + mapContainer.mapY;
        }
    }

    public MapContainer mapContainer;

    public LayoutCoordinate(MapCoordinate coodrinate) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        x = (int)coodrinate.x / constants.featuresPerLayoutPerAxis;
        y = (int)coodrinate.y / constants.featuresPerLayoutPerAxis;

        mapContainer = coodrinate.mapContainer;
    }

    public LayoutCoordinate(int layoutX, int layoutY, MapContainer mapContainer) {
        x = layoutX;
        y = layoutY;

        this.mapContainer = mapContainer;
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

    public string description {
        get {
            return "x: " + x + " y: " + y;
        }
    }

    public static PathGridCoordinate fromMapCoordinate(MapCoordinate mapCoordinate) {
        return Script.Get<MapsManager>().PathGridCoordinateFromMapCoordinate(mapCoordinate);
    }

    //public PathGridCoordinate(MapCoordinate mapCoordinate) {
    //    Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

    //    x = mapCoordinate.x / constants.featuresPerLayoutPerAxis * constants.nodesPerLayoutPerAxis;
    //    y = mapCoordinate.y / constants.featuresPerLayoutPerAxis * constants.nodesPerLayoutPerAxis;
    //}

    public static PathGridCoordinate[][] pathCoordiatesFromLayoutCoordinate (LayoutCoordinate layoutCoordinate) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        List<PathGridCoordinate[]> pathColumns = new List<PathGridCoordinate[]>();

        int mapPostionX = layoutCoordinate.mapContainer.mapX * constants.layoutMapWidth * constants.nodesPerLayoutPerAxis;
        int mapPostionY = layoutCoordinate.mapContainer.mapY * constants.layoutMapHeight * constants.nodesPerLayoutPerAxis;

        for(int x = mapPostionX + layoutCoordinate.x * constants.nodesPerLayoutPerAxis; x < mapPostionX + (layoutCoordinate.x + 1) * constants.nodesPerLayoutPerAxis; x++) {
            List<PathGridCoordinate> pathCoordinates = new List<PathGridCoordinate>();

            for(int y = mapPostionY + layoutCoordinate.y * constants.nodesPerLayoutPerAxis; y < mapPostionY + (layoutCoordinate.y + 1) * constants.nodesPerLayoutPerAxis; y++) {
                pathCoordinates.Add(new PathGridCoordinate(x, y));
            }

            pathColumns.Add(pathCoordinates.ToArray());
        }

        return pathColumns.ToArray();
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

    public MapContainer mapContainer;

    public string description {
        get {
            return "x: " + x + " y: " + y + ", of map X " + mapContainer.mapX + " Y " + mapContainer.mapY;
        }
    }

    public static MapCoordinate FromGridCoordinate(PathGridCoordinate pathGridCoordinate) {
        return Script.Get<MapsManager>().MapCoordinateFromPathGridCoordinate(pathGridCoordinate);
    }

    //public MapCoordinate(PathGridCoordinate pathGridCoordinate) {
    //    Constants constants = Script.Get<Constants>();

    //    x = pathGridCoordinate.x / constants.nodesPerLayoutPerAxis * constants.featuresPerLayoutPerAxis;
    //    y = pathGridCoordinate.y / constants.nodesPerLayoutPerAxis * constants.featuresPerLayoutPerAxis;

    //}

    public MapCoordinate(LayoutCoordinate layoutCoordinate) {
        Constants constants = Script.Get<Constants>();
        x = layoutCoordinate.x * constants.featuresPerLayoutPerAxis + (constants.featuresPerLayoutPerAxis / 2f);
        y = layoutCoordinate.y * constants.featuresPerLayoutPerAxis + (constants.featuresPerLayoutPerAxis / 2f);

        mapContainer = layoutCoordinate.mapContainer;
    }

    public static MapCoordinate FromWorldPosition(WorldPosition worldPosition) {

        return Script.Get<MapsManager>().MapCoordinateFromWorld(worldPosition);

        //GameObject map = Tag.Map.GetGameObject();
        //Transform mapObjectSpace = map.transform;
        //Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        //Vector3 mapPosition = mapObjectSpace.InverseTransformPoint(worldPosition.vector3);

        //x = mapPosition.x + constants.mapWidth / 2f;
        //y = - mapPosition.z + constants.mapHeight / 2f;
    }
    
    public MapCoordinate(float x, float y, MapContainer mapContainer) {
        this.x = x;
        this.y = y;

        this.mapContainer = mapContainer;
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

    public string description {
        get {
            return "x: " + x + " y: " + y + " z: " + z;
        }
    }

    public WorldPosition(Vector3 position) {
        x = position.x;
        y = position.y;
        z = position.z;
    }    

    //public static WorldPosition FromGridCoordinate(PathGridCoordinate pathGridCoordinate) {
    //    return Script.Get<MapsManager>().mapCoordinateFromPathGridCoordinate(pathGridCoordinate);
    //}

    public WorldPosition(MapCoordinate mapCoordinate) {
        MapContainer mapContainer = mapCoordinate.mapContainer;
        Map map = mapContainer.map;
        Constants constants = Script.Get<Constants>();

        Transform mapObjectSpace = mapContainer.transform;

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
        this = new WorldPosition(MapCoordinate.FromWorldPosition(this));
    }
}