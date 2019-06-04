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

    public string description => "The World? What should go here";

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

        MapCoordinate mapCoordinate = new MapCoordinate(coordinate);
        WorldPosition worldPosition = new WorldPosition(mapCoordinate);

        // Can I...

        List<UserAction> actionList = new List<UserAction>();

        // Build?
        if (GetTerrainAt(coordinate).regionType == RegionType.Land) {
            foreach (Building.Blueprint blueprint in Building.Blueprints()) {
                actionList.Add(blueprint.ConstructionAction(worldPosition));
            }
        }

        // Mine?
        if(GetTerrainAt(coordinate).regionType == RegionType.Mountain) {
            UserAction action = new UserAction();

            TerrainType landTerrain = Script.Get<MapGenerator>().TerrainForRegion(RegionType.Land);

            action.description = "Mine Wall";
            action.performAction = () => {
                TaskQueueManager queue = Script.Get<TaskQueueManager>();

                GameTask miningTask = new GameTask(worldPosition, GameTask.ActionType.Mine, this, PathRequestTargetType.Layout);
                MasterGameTask masterMiningTask = new MasterGameTask("Mine at location " + coordinate.description, new GameTask[] { miningTask });

                queue.QueueTask(masterMiningTask);                
            };

            actionList.Add(action);
        }


        // TEST ACTIONS
        UserAction testAction1 = new UserAction();

        testAction1.description = "Find Any Ore";
        testAction1.performAction = () => {
            TaskQueueManager queue = Script.Get<TaskQueueManager>();

            GameTask oreTask = new GameTask(GameResourceManager.GatherType.Ore, GameTask.ActionType.PickUp, null);

            MasterGameTask masterOreTask = new MasterGameTask("Gather Ore Somwhere ", new GameTask[] { oreTask });

            queue.QueueTask(masterOreTask);
        };

        actionList.Add(testAction1);



        return actionList.ToArray();
    }

    //public void UpdateTerrain(TerrainType terrain, LayoutCoordinate coordinate) {
    //    MapGenerator mapGenerator = Script.Get<MapGenerator>();

    //    finalHeightMap = mapGenerator.TerraformHeightMap(layoutNoiseMap, featuresNoiseMap, 0.4f, coordinate);

    //    meshData = MeshGenerator.UpdateTerrainMesh(meshData, finalHeightMap, featuresPerLayoutPerAxis, coordinate);

    //    Script.Get<MapContainer>().DrawMesh();
    //    terrainData[coordinate.x, coordinate.y] = terrain;

    //    Script.Get<PathfindingGrid>().UpdateGrid(this, coordinate);

    //    foreach (TerrainUpdateDelegate updateDelegate in terrainUpdateDelegates) {
    //        updateDelegate.NotifyTerrainUpdate();
    //    }
    //}

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

    public float performAction(GameTask task, float rate, Unit unit) {

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

            // Terraform complete

            // Update the terrain type at this location
            terrainData[terraformTarget.coordinate.x, terraformTarget.coordinate.y] = terraformTarget.terrainTypeTarget;

            // Update pathfinding grid
            Script.Get<PathfindingGrid>().UpdateGrid(this, terraformTarget.coordinate);

            // Notify all users of path finding grid about ubdate
            foreach(TerrainUpdateDelegate updateDelegate in terrainUpdateDelegates) {
                updateDelegate.NotifyTerrainUpdate();
            }

            // Create Ore at location

            //Ore ore = OreManager.Blueprint.Basic.Instantiate() as Ore;
            Ore ore = GameResourceManager.sharedInstance.CreateOre();

            Vector3 position = task.target.vector3;

            ore.transform.position = position;


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
