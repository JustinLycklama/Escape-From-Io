using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TerrainUpdateDelegate {
    void NotifyTerrainUpdate();
}

public class Map {
    public int mapWidth;
    public int mapHeight;
    public Vector2 textureMapSize;

    public MeshData meshData;
    public Texture2D meshTexture;

    private TerrainType[,] terrainData;
    private Building[,] buildingData;

    int featuresPerLayoutPerAxis;

    float[,] layoutNoiseMap;
    float[,] featuresNoiseMap;

    float[,] finalHeightMap;

    List<TerrainUpdateDelegate> terrainUpdateDelegates;

    public Map(float[,] finalHeightMap, float[,] layoutNoiseMap, float[,] featuresNoiseMap,
        int featuresPerLayoutPerAxis, MeshData meshData, Texture2D meshTexture, TerrainType[,] terrainData) {

        mapWidth = finalHeightMap.GetLength(0);
        mapHeight = finalHeightMap.GetLength(1);

        this.textureMapSize = new Vector2(mapWidth, mapHeight);

        this.featuresPerLayoutPerAxis = featuresPerLayoutPerAxis;

        this.finalHeightMap = finalHeightMap;

        this.layoutNoiseMap = layoutNoiseMap;
        this.featuresNoiseMap = featuresNoiseMap;

        this.meshData = meshData;
        this.meshTexture = meshTexture;
        this.terrainData = terrainData;

        buildingData = new Building[terrainData.GetLength(0), terrainData.GetLength(1)];
        terrainUpdateDelegates = new List<TerrainUpdateDelegate>();
    }

    public TerrainType GetTerrainAt(LayoutCoordinate layoutCoordinate) {
        return terrainData[layoutCoordinate.x, layoutCoordinate.y];
    }

    public Building GetBuildingAt(LayoutCoordinate layoutCoordinate) {
        return buildingData[layoutCoordinate.x, layoutCoordinate.y];
    }

    public float getHeightAt(MapCoordinate coordinate) {
        int mapWidth = finalHeightMap.GetLength(0);
        int mapHeight = finalHeightMap.GetLength(1);

        // If I have a map coordinate, should it be guarenteed to be on the map?
        //if (coordinate.x < 0 || coordinate.y < 0 || coordinate.x >= mapWidth || coordinate.y >= mapHeight) {
        //    return 0;
        //}

        // TODO: Triangle Calculations        
       
        return finalHeightMap[coordinate.xLowSample, coordinate.yLowSample];
    }

    public void AddTerrainUpdateDelegate(TerrainUpdateDelegate updateDelegate) {
        terrainUpdateDelegates.Add(updateDelegate);
    }

    public void RemoveTerrainUpdateDelegate(TerrainUpdateDelegate updateDelegate) {
        terrainUpdateDelegates.Remove(updateDelegate);
    }

    public UserAction[] ActionsAvailableAt(LayoutCoordinate coordinate) {
        // Can I...

        List<UserAction> actionList = new List<UserAction>();

        // Build?
        if (GetTerrainAt(coordinate).regionType == RegionType.Land) {
            UserAction action = new UserAction();

            action.description = "Build This building";
            action.performAction = () => {
                //Building building = new Building();
                MapCoordinate mapCoordinate = new MapCoordinate(coordinate);
                WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                worldPosition.y += 25 / 2f;

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = worldPosition.vector3;

                Material newMat = Resources.Load("BuildingMaterial", typeof(Material)) as Material;
                cube.GetComponent<MeshRenderer>().material = newMat;

                cube.AddComponent<Building>();

                cube.transform.localScale = new Vector3(25, 25, 25);

                TaskQueue queue = Script.Get<TaskQueue>();
                queue.QueueTask(new GameTask(worldPosition, GameAction.Build, cube.GetComponent<Building>()));
            };

            actionList.Add(action);
        }

        // Mine?
        if(GetTerrainAt(coordinate).regionType == RegionType.Mountain) {
            UserAction action = new UserAction();

            TerrainType landTerrain = Script.Get<MapGenerator>().TerrainForRegion(RegionType.Land);

            action.description = "Mine Wall";
            action.performAction = () => {
                UpdateTerrain(landTerrain, coordinate);
            };

            actionList.Add(action);
        }

        return actionList.ToArray();
    }

    public void UpdateTerrain(TerrainType terrain, LayoutCoordinate coordinate) {
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        finalHeightMap = mapGenerator.TerraformHeightMap(layoutNoiseMap, featuresNoiseMap, coordinate, terrain);

        meshData = MeshGenerator.UpdateTerrainMesh(meshData, finalHeightMap, featuresPerLayoutPerAxis, coordinate);

        Script.Get<MapContainer>().DrawMesh();
        terrainData[coordinate.x, coordinate.y] = terrain;

        Script.Get<PathfindingGrid>().UpdateGrid(this, coordinate);

        foreach (TerrainUpdateDelegate updateDelegate in terrainUpdateDelegates) {
            updateDelegate.NotifyTerrainUpdate();
        }
    }   
}
