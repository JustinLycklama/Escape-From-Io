using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TerrainUpdateDelegate {
    void NotifyTerrainUpdate();
}

public class Map : ActionableItem {
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
    Dictionary<GameTask, TerraformTarget> terraformTargetDictionary;

    public override string description => "The World? What should go here";

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
        terraformTargetDictionary = new Dictionary<GameTask, TerraformTarget>();
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
                TaskQueue queue = Script.Get<TaskQueue>();

                MapCoordinate mapCoordinate = new MapCoordinate(coordinate);
                WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                queue.QueueTask(new GameTask(worldPosition, GameAction.Mine, this, PathRequestTargetType.Layout));

                //UpdateTerrain(landTerrain, coordinate);
            };

            actionList.Add(action);
        }

        return actionList.ToArray();
    }

    public void UpdateTerrain(TerrainType terrain, LayoutCoordinate coordinate) {
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        finalHeightMap = mapGenerator.TerraformHeightMap(layoutNoiseMap, featuresNoiseMap, 0.4f, coordinate);

        meshData = MeshGenerator.UpdateTerrainMesh(meshData, finalHeightMap, featuresPerLayoutPerAxis, coordinate);

        Script.Get<MapContainer>().DrawMesh();
        terrainData[coordinate.x, coordinate.y] = terrain;

        Script.Get<PathfindingGrid>().UpdateGrid(this, coordinate);

        foreach (TerrainUpdateDelegate updateDelegate in terrainUpdateDelegates) {
            updateDelegate.NotifyTerrainUpdate();
        }
    }

    // Actionable Item Interface

    class TerraformTarget {
        public LayoutCoordinate coordinate;
        public TerrainType terrainTypeTarget;

        public float heightTarget;
        public float initialHeight;

        public float percentage;


        public TerraformTarget(LayoutCoordinate coordinate, TerrainType terrainTypeTarget, float heightTarget, float initialHeight) {
            this.coordinate = coordinate;
            this.terrainTypeTarget = terrainTypeTarget;
            this.heightTarget = heightTarget;
            this.initialHeight = initialHeight;

            this.percentage = 0;
        }

    }

    public override float performAction(GameTask task, float rate) {

        TerraformTarget terraformTarget;

        if (terraformTargetDictionary.ContainsKey(task)) {
            terraformTarget = terraformTargetDictionary[task];
        } else {
            MapCoordinate mapCoordinate = new MapCoordinate(task.target);
            LayoutCoordinate coordinate = new LayoutCoordinate(mapCoordinate);

            TerrainType targetTerrain = Script.Get<MapGenerator>().TerrainForRegion(RegionType.Land);

            if (GetTerrainAt(coordinate) == targetTerrain) {
                // Do not attempt to terraform a target point into the same terrain type
                return 1;
            }

            float targetTerrainHeight = targetTerrain.plateauAtBase ? targetTerrain.noiseBaseline : targetTerrain.noiseMax;

            terraformTarget = new TerraformTarget(coordinate, targetTerrain, targetTerrainHeight, layoutNoiseMap[coordinate.x, coordinate.y]);
            terraformTargetDictionary[task] = terraformTarget;
        }

        terraformTarget.percentage += rate;
        if (terraformTarget.percentage >= 1) {
            terraformTarget.percentage = 1;

            terrainData[terraformTarget.coordinate.x, terraformTarget.coordinate.y] = terraformTarget.terrainTypeTarget;

            Script.Get<PathfindingGrid>().UpdateGrid(this, terraformTarget.coordinate);

            foreach(TerrainUpdateDelegate updateDelegate in terrainUpdateDelegates) {
                updateDelegate.NotifyTerrainUpdate();
            }

            terraformTargetDictionary.Remove(task);
        }

        float currentHeightAtCoordinate = Mathf.Lerp(terraformTarget.initialHeight, terraformTarget.heightTarget, terraformTarget.percentage);

        MapGenerator mapGenerator = Script.Get<MapGenerator>();
        finalHeightMap = mapGenerator.TerraformHeightMap(layoutNoiseMap, featuresNoiseMap, currentHeightAtCoordinate, terraformTarget.coordinate);

        meshData = MeshGenerator.UpdateTerrainMesh(meshData, finalHeightMap, featuresPerLayoutPerAxis, terraformTarget.coordinate);
        Script.Get<MapContainer>().DrawMesh();

        return terraformTarget.percentage;
    }
}
