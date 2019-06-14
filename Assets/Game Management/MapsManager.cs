using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TerrainUpdateDelegate {
    void NotifyTerrainUpdate();
}

public class MapsManager : MonoBehaviour {
    int horizontalMapCount;
    int verticalMapCount;

    Rect mapsBoundaries;

    public List<MapContainer> mapContainers;
    public MapContainer[,] mapContainer2d;

    List<TerrainUpdateDelegate> terrainUpdateDelegates;

    void Awake() {
        mapContainers = new List<MapContainer>();
        terrainUpdateDelegates = new List<TerrainUpdateDelegate>();
    }

    //public MapContainer[] GetMapContainers() {
    //    return mapContainers.ToArray();
    //}

    public void InitializeMaps(int horizontalMaps, int verticalMaps) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        horizontalMapCount = horizontalMaps;
        verticalMapCount = verticalMaps;
     
        mapsBoundaries = new Rect(Vector2.zero, new Vector2(constants.mapWidth * horizontalMapCount, constants.mapHeight * verticalMapCount));

        mapContainers.Clear();
        mapContainer2d = new MapContainer[horizontalMaps, verticalMaps];

        Object mapResource = Resources.Load("Map", typeof(MapContainer));

        float mapEdgeX = mapsBoundaries.x - (mapsBoundaries.width / 2f);
        float mapEdgeY = mapsBoundaries.y - (mapsBoundaries.height / 2f);

        for(int x = 0; x < horizontalMaps; x++) {        
            for(int y = 0; y < verticalMaps; y++) {
                // We need to create a Map Rect in MAPS MANAGER SPACE
                Vector2 mapPoint = new Vector2(mapEdgeX + (constants.mapWidth * x) , mapEdgeY + (constants.mapHeight * y));                
                Rect mapRect = new Rect(mapPoint, new Vector2(constants.mapWidth, constants.mapHeight));

                MapContainer mapContainer = Instantiate(mapResource) as MapContainer;                

                mapContainer.SetMapPosition(x, y, mapRect);

                Vector3 MapsManagerSpacePosition = new Vector3(mapPoint.x + (constants.mapWidth / 2f), 0, mapPoint.y + (constants.mapHeight / 2f));
                mapContainer.transform.position = transform.TransformPoint(MapsManagerSpacePosition);
                mapContainer.transform.localScale = transform.localScale;

                mapContainer2d[x, y] = mapContainer;
                mapContainers.Add(mapContainer);
            }
        }
    }

    public void AddTerrainUpdateDelegate(TerrainUpdateDelegate updateDelegate) {
        terrainUpdateDelegates.Add(updateDelegate);
    }

    public void RemoveTerrainUpdateDelegate(TerrainUpdateDelegate updateDelegate) {
        terrainUpdateDelegates.Remove(updateDelegate);
    }

    public void NotifyTerrainUpdateDelegates() {
        // Notify all users of path finding grid about ubdate
        foreach(TerrainUpdateDelegate updateDelegate in terrainUpdateDelegates) {
            updateDelegate.NotifyTerrainUpdate();
        }
    }

    public float GetHeightAt(MapCoordinate mapCoordinate) {
        return mapCoordinate.mapContainer.map.getHeightAt(mapCoordinate);
    }

    public TerrainType GetTerrainAt(LayoutCoordinate layoutCoordinate) {
        return layoutCoordinate.mapContainer.map.GetTerrainAt(layoutCoordinate);
    }

    public UserAction[] ActionsAvailableAt(LayoutCoordinate layoutCoordinate) {
        return layoutCoordinate.mapContainer.map.ActionsAvailableAt(layoutCoordinate);
    }

    public Material GetMaterialForMap(LayoutCoordinate layoutCoordinate) {
        return layoutCoordinate.mapContainer.GetComponent<MeshRenderer>().material;
    }



    // MAP STRUCTS
    //public PathGridCoordinate PathGridCoordinateFromWorld(WorldPosition worldPosition) {

    //}

    public WorldPosition WorldPositionFromPathGridCoordinate(PathGridCoordinate pathGridCoordinate) {
        Constants constants = Script.Get<Constants>();

        // Path grid converts into the "Maps Manager Object Space' beause there is one grid for all maps, not a grid per map
        // To get into MapsManager space, x and y need to be centered at 0,0, not starting at 0,0
        float mapsManagerXCoord = pathGridCoordinate.x / constants.nodesPerLayoutPerAxis * constants.featuresPerLayoutPerAxis - (mapsBoundaries.width / 2f);
        float mapsManagerYCoord = pathGridCoordinate.y / constants.nodesPerLayoutPerAxis * constants.featuresPerLayoutPerAxis - (mapsBoundaries.height / 2f);

        MapContainer mapContainer = MapContainerForPoint(new Vector2(mapsManagerXCoord, mapsManagerYCoord));

        // We do not know the coordinates for the map we are looking at right now, so we can't create a map coordinate
        Vector3 currentPointObjectSpace = new Vector3(mapsManagerXCoord, 0, mapsManagerYCoord);
        Vector3 positionWorldSpace = transform.TransformPoint(currentPointObjectSpace);

        // Now we create a mapCoordinate to figure out the height
        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(new WorldPosition(positionWorldSpace));

        // And we can finally send back the correct world position with height
        return new WorldPosition(mapCoordinate);


        //MapCoordinate mapCoordinate = new MapCoordinate(mapsManagerXCoord, mapsManagerYCoord, mapContainer);

        // At this point we do not know the height yet, we do not know what map we are dealing with
        //Vector3 currentPointObjectSpace = new Vector3(x, mapContainer.map.getHeightAt(mapCoordinate), y);

        //return new WorldPosition(mapCoordinate);

        //currentPointObjectSpace.x -= (constants.mapWidth / 2f);
        //currentPointObjectSpace.z *= -1;
        //currentPointObjectSpace.z += (constants.mapHeight / 2f);

        //Vector3 positionWorldSpace = mapContainer.transform.TransformPoint(currentPointObjectSpace);
        //WorldPosition worldPosition = new WorldPosition(positionWorldSpace);

        //MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

        //// Do the calculation again with the correct height
        //currentPointObjectSpace = new Vector3(x, mapContainer.map.getHeightAt(mapCoordinate), y);

        //currentPointObjectSpace.x -= (constants.mapWidth / 2f);
        //currentPointObjectSpace.z *= -1;
        //currentPointObjectSpace.z += (constants.mapHeight / 2f);

        //positionWorldSpace = mapContainer.transform.TransformPoint(currentPointObjectSpace);
        //worldPosition = new WorldPosition(positionWorldSpace);

        //return worldPosition;
    }

    //public WorldPosition WorldPositionFromMapCoordinate(MapCoordinate) {

    //}

    public MapCoordinate MapCoordinateFromWorld(WorldPosition worldPosition) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition.vector3);
        Vector2 localPoint = new Vector2(localPosition.x, localPosition.z);

        MapContainer map = MapContainerForPoint(localPoint);

        Vector3 mapPosition = map.transform.InverseTransformPoint(worldPosition.vector3);

        float x = mapPosition.x + constants.mapWidth / 2f;
        float y = -mapPosition.z + constants.mapHeight / 2f;

        return new MapCoordinate(x, y, map);
    }

    // Vector2 Point is in MapsManager Space, i.e the CONTAINER for all Map Objects, right before World Space
    private MapContainer MapContainerForPoint(Vector2 point) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        MapContainer map = null;
        for(int mapX = 0; mapX < horizontalMapCount; mapX++) {
            for(int mapY = 0; mapY < verticalMapCount; mapY++) {
                MapContainer mapContainer = mapContainer2d[mapX, mapY];

                if(mapContainer.mapRect.Contains(point)) {
                    return mapContainer2d[mapX, mapY];
                }
            }
        }

        if (map == null) {
            print("Null");
        }

        for(int mapX = 0; mapX < horizontalMapCount; mapX++) {
            for(int mapY = 0; mapY < verticalMapCount; mapY++) {
                MapContainer mapContainer = mapContainer2d[mapX, mapY];

                if(mapContainer.mapRect.Contains(point)) {
                    return mapContainer2d[mapX, mapY];
                }
            }
        }

        return map;
    }

}
