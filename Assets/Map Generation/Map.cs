using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : ActionableItem  {

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

            percentage = 0;
        }
    }

    // Map Width + Height in Map Coordinates (Not Layout Coordinates)
    public int mapWidth;
    public int mapHeight;
    //public Vector2 textureMapSize;

    public MeshData meshData;
    public Texture2D meshTexture;

    private TerrainType[,] terrainData;
    private Building[,] buildingData;

    public int featuresPerLayoutPerAxis;

    float[,] layoutNoiseMap;
    float[,] groundFeaturesNoiseMap;
    float[,] mountainFeaturesNoiseMap;

    float[,] finalHeightMap;

    public MapContainer mapContainer;

    TerraformTarget[,] terraformTargetCoordinateMap;

    public string description => "The World? What should go here";

    public void InitMap(TerrainType[,] terrainData, float[,] finalHeightMap, float[,] layoutNoiseMap, float[,] groundFeaturesNoiseMap, float[,] mountainFeaturesNoiseMap, MeshData meshData) {
        Constants constants = Script.Get<Constants>();
        featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;

        mapWidth = finalHeightMap.GetLength(0);
        mapHeight = finalHeightMap.GetLength(1);

        //this.textureMapSize = new Vector2(mapWidth, mapHeight);


        this.finalHeightMap = finalHeightMap;

        this.layoutNoiseMap = layoutNoiseMap;
        this.groundFeaturesNoiseMap = groundFeaturesNoiseMap;
        this.mountainFeaturesNoiseMap = mountainFeaturesNoiseMap;

        this.meshData = meshData;
        //this.meshTexture = meshTexture;
        this.terrainData = terrainData;

        int terrainWidth = terrainData.GetLength(0);
        int terrainHeight = terrainData.GetLength(1);

        buildingData = new Building[terrainWidth, terrainHeight];
        terraformTargetCoordinateMap = new TerraformTarget[terrainWidth, terrainHeight];
    }

    public TerrainType GetTerrainAt(LayoutCoordinate layoutCoordinate) {
        if (layoutCoordinate.x >= terrainData.GetLength(0) || layoutCoordinate.y >= terrainData.GetLength(1)) {
            throw new MissingReferenceException();
        }

        return terrainData[layoutCoordinate.x, layoutCoordinate.y];
    }

    public Building GetBuildingAt(LayoutCoordinate layoutCoordinate) {
        return buildingData[layoutCoordinate.x, layoutCoordinate.y];
    }

    // Returns the height in MAP COORDINATE position
    public float getHeightAt(MapCoordinate coordinate) {
        int mapWidth = finalHeightMap.GetLength(0);
        int mapHeight = finalHeightMap.GetLength(1);

        // If I have a map coordinate, should it be guarenteed to be on the map?
        if(coordinate.x < 0 || coordinate.y < 0 || coordinate.x >= mapWidth || coordinate.y >= mapHeight) {
            return 10;
        }

        // TODO: Triangle Calculations        

        return finalHeightMap[coordinate.xLowSample, coordinate.yLowSample];
    }

    //Dictionary<LayoutCoordinate, UserAction[]> userActionDictionary = new Dictionary<LayoutCoordinate, UserAction[]>();
    //Dictionary<LayoutCoordinate, List<UserActionUpdateDelegate>> userActionDelegateMap = new Dictionary<LayoutCoordinate, List<UserActionUpdateDelegate>>();

    /*
     * Actionable Item Override Properties
     * */

    UserAction[,][] userActionCoordinateMap;
    List<UserActionUpdateDelegate>[,] userActionDelegateMap;

    private MasterGameTask[,] associatedTasksCoordinateMap;
    private List<TaskStatusUpdateDelegate>[,] taskUpdateDelegateMap;


    public void CreateAllActionableItemOverrides() {

        int width = terrainData.GetLength(0);
        int height = terrainData.GetLength(1);

        userActionCoordinateMap = new UserAction[width, height][];
        associatedTasksCoordinateMap = new MasterGameTask[width, height];

        userActionDelegateMap = new List<UserActionUpdateDelegate>[width, height];
        taskUpdateDelegateMap = new List<TaskStatusUpdateDelegate>[width, height];

        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, mapContainer);

                userActionCoordinateMap[x, y] = ActionsAvailableAt(layoutCoordinate);
                associatedTasksCoordinateMap[x, y] = null;

                userActionDelegateMap[x, y] = new List<UserActionUpdateDelegate>();
                taskUpdateDelegateMap[x, y] = new List<TaskStatusUpdateDelegate>();
            }
        }
    }

    private void UpdateUserActionsAt(LayoutCoordinate layoutCoordinate) {
        userActionCoordinateMap[layoutCoordinate.x, layoutCoordinate.y] = ActionsAvailableAt(layoutCoordinate);

        NotifyAllUserActionsUpdate(layoutCoordinate);
    }

    private UserAction[] ActionsAvailableAt(LayoutCoordinate coordinate) {

        MapCoordinate mapCoordinate = new MapCoordinate(coordinate);
        WorldPosition worldPosition = new WorldPosition(mapCoordinate);

        TerrainManager terrainManager = Script.Get<TerrainManager>();

        // Can I...

        List<UserAction> actionList = new List<UserAction>();

        if(associatedTasksCoordinateMap[coordinate.x, coordinate.y] != null) {
            UserAction action = new UserAction();

            action.description = "Cancel";
            action.layoutCoordinate = coordinate;

            action.performAction = (LayoutCoordinate layoutCoordinate) => {
                associatedTasksCoordinateMap[layoutCoordinate.x, layoutCoordinate.y].CancelTask();
            };

            actionList.Add(action);
        } else {
            return terrainManager.ActionsAvailableForTerrain(coordinate);


            // Build?
        }

        //if (GetTerrainAt(coordinate).buildable) {

        //    UserAction action = new UserAction();
        //    action.description = "Building";
        //    action.layoutCoordinate = coordinate;

        //    action.blueprintList = new ConstructionBlueprint[] { Building.Blueprint.Tower, Building.Blueprint.Refinery };

        //    actionList.Add(action);

            //foreach (Building.Blueprint blueprint in Building.Blueprints()) {
            //    actionList.Add(blueprint.ConstructionAction(worldPosition));

            //UserAction action = new UserAction();

            //TerrainType landTerrain = Script.Get<MapGenerator>().TerrainForRegion(RegionType.Land);

            //action.description = "Add Path";
            //action.performAction = () => {
            //    TaskQueueManager queue = Script.Get<TaskQueueManager>();

            //    GameTask miningTask = new GameTask("Building", worldPosition, GameTask.ActionType.FlattenPath, this, PathRequestTargetType.Layout);
            //    MasterGameTask masterMiningTask = new MasterGameTask(MasterGameTask.ActionType.Build, "Build Path At " + coordinate.description, new GameTask[] { miningTask });

            //    queue.QueueTask(masterMiningTask);


            //};

            //actionList.Add(action);
            //}
        //} 
        // We cannot build but we are a land, lets terraform to buildable
        //else if (false) {

        //}

        //// Mine?
        //else if(GetTerrainAt(coordinate).regionType == RegionType.Type.Mountain) {
        //    UserAction action = new UserAction();

        //    //TerrainType landTerrain = Script.Get<MapGenerator>().TerrainForRegion(RegionType.Type.Land);

        //    action.description = "Mine Wall";
        //    action.layoutCoordinate = coordinate;

        //    action.performAction = (LayoutCoordinate layoutCoordinate) => {
        //        TaskQueueManager queue = Script.Get<TaskQueueManager>();

        //        GameTask miningTask = new GameTask("Mining", worldPosition, GameTask.ActionType.Mine, this, PathRequestTargetType.Layout);
        //        MasterGameTask masterMiningTask = new MasterGameTask(MasterGameTask.ActionType.Mine, "Mine at location " + layoutCoordinate.description, new GameTask[] { miningTask });

        //        queue.QueueTask(masterMiningTask);
        //        this.AssociateTask(masterMiningTask, layoutCoordinate);
        //    };

        //    actionList.Add(action);
        //}

        return actionList.ToArray();
    }

    /*
     * Actionable Item Interface
     * */
    public override float performAction(GameTask task, float rate, Unit unit) {

        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(task.target);
        LayoutCoordinate coordinate = new LayoutCoordinate(mapCoordinate);

        TerraformTarget terraformTarget;

        if (terraformTargetCoordinateMap[coordinate.x, coordinate.y] != null) {
            terraformTarget = terraformTargetCoordinateMap[coordinate.x, coordinate.y];
        } else {
            TerrainManager terrainManager = Script.Get<TerrainManager>();
            TerrainType originalTerrain = GetTerrainAt(coordinate);

            TerrainType? targetTerrain = terrainManager.CanTerriformTo(originalTerrain);

            //if (task.action == GameTask.ActionType.FlattenPath) {
            //    targetTerrain = Script.Get<MapGenerator>().TerrainForRegion(RegionType.Path);
            //}

            if (targetTerrain == null) {
                // Do not attempt to terraform a target point into the same terrain type
                return 1;
            }

            RegionType regionType = terrainManager.regionTypeMap[targetTerrain.Value.regionType];
            float targetTerrainHeight = regionType.plateauAtBase ? regionType.noiseBase : regionType.noiseMax;

            terraformTarget = new TerraformTarget(coordinate, targetTerrain.Value, targetTerrainHeight, layoutNoiseMap[coordinate.x, coordinate.y]);
            terraformTargetCoordinateMap[coordinate.x, coordinate.y] = terraformTarget;
        }

        terraformTarget.percentage += rate * GetTerrainAt(coordinate).modificationSpeedModifier;
        if (terraformTarget.percentage >= 1) {
            terraformTarget.percentage = 1;

            // Terraform complete

            // Update the terrain type at this location
            terrainData[terraformTarget.coordinate.x, terraformTarget.coordinate.y] = terraformTarget.terrainTypeTarget;

            TerraformHeightMap(terraformTarget);

            // Update the BoxCollider Height
            mapContainer.ResizeBoxColliderAt(terraformTarget.coordinate);

            // Update pathfinding grid
            Script.Get<PathfindingGrid>().UpdateGrid(this, terraformTarget.coordinate);

            // Notify all users of path finding grid about ubdate
            Script.Get<MapsManager>().NotifyTerrainUpdateDelegates();

            // Create Ore at location

            Vector3 position = new WorldPosition(new MapCoordinate (terraformTarget.coordinate)).vector3;

            float offset = 25;

            Vector3 position1 = position + new Vector3(offset, 0, 0);
            Vector3 position2 = position + new Vector3(0, 0, offset);
            Vector3 position3 = position + new Vector3(-offset, 0, 0);

            Vector3[] positions = new Vector3[] { position1, position2, position3 };

            //int rInt = Random.Range(1, 3);

            GameResourceManager resourceManager = Script.Get<GameResourceManager>();

            Dictionary<MineralType, int> mineralLists = resourceManager.MineralListForCoordinate(coordinate);

            int positionIndex = 0;
            foreach (MineralType mineralType in mineralLists.Keys) {
                int count = mineralLists[mineralType];

                Ore ore = resourceManager.CreateMineral(mineralType);
                ore.transform.position = positions[positionIndex];
                positionIndex++;
            }

            terraformTargetCoordinateMap[coordinate.x, coordinate.y] = null;
            UpdateUserActionsAt(terraformTarget.coordinate);
            mapContainer.UpdateShaderTerrainTextures();
        } else {
            TerraformHeightMap(terraformTarget);
        }

        return terraformTarget.percentage;
    }

    private void TerraformHeightMap(TerraformTarget terraformTarget) {
        float currentHeightAtCoordinate = Mathf.Lerp(terraformTarget.initialHeight, terraformTarget.heightTarget, terraformTarget.percentage);

        MapGenerator mapGenerator = Script.Get<MapGenerator>();
        finalHeightMap = mapGenerator.TerraformHeightMap(layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap, terrainData, currentHeightAtCoordinate, terraformTarget.coordinate);

        MeshGenerator.UpdateTerrainMesh(meshData, finalHeightMap, featuresPerLayoutPerAxis, terraformTarget.coordinate);
        mapContainer.DrawMesh();

        int layoutWidth = (mapWidth / featuresPerLayoutPerAxis);
        int layoutHeight = (mapHeight / featuresPerLayoutPerAxis);

        // Update neibour overhang
        if(terraformTarget.coordinate.x == layoutWidth - 1) {

            if(terraformTarget.coordinate.y == 0) {
                if(mapContainer.neighbours.topRightMap != null) {
                    mapContainer.neighbours.topRightMap.UpdateMapOverhang();
                }
            } else if(terraformTarget.coordinate.y == layoutHeight - 1) {
                if(mapContainer.neighbours.bottomRightMap != null) {
                    mapContainer.neighbours.bottomRightMap.UpdateMapOverhang();
                }
            }

            if (mapContainer.neighbours.rightMap != null) {
                mapContainer.neighbours.rightMap.UpdateMapOverhang();
            }
        }

        if (terraformTarget.coordinate.y == layoutHeight - 1 && mapContainer.neighbours.bottomMap != null) {
            mapContainer.neighbours.bottomMap.UpdateMapOverhang();
        }
    }

    /*
     * Invalud ActionableItem Components
     * */

    public override void AssociateTask(MasterGameTask task) {
        if (task == null) {
            return;
        }

        foreach(GameTask gameTask in task.childGameTasks) {
            UpdateMasterTaskByGameTask(gameTask, task);
        }
    }

    // Any time we try to remove associated MasterTask without a layout coordinate, we find all relevant GameTasks and remove at those coordinates
    public override void UpdateMasterTaskByGameTask(GameTask gameTask, MasterGameTask masterGameTask) {
        if (gameTask.actionItem == this) {
            LayoutCoordinate layoutCoordinate = new LayoutCoordinate(MapCoordinate.FromWorldPosition(gameTask.target));

            AssociateTask(masterGameTask, layoutCoordinate);
        }        
    }

    /*
     * ActionableItem Components
     * */

    public void AssociateTask(MasterGameTask task, LayoutCoordinate coordinate) {
        associatedTasksCoordinateMap[coordinate.x, coordinate.y] = task;

        UpdateUserActionsAt(coordinate);
        NotifyAllTaskStatus(coordinate);
    }

    /*
     * Map Version Of TaskStatusNotifiable Interface
     * */

    public void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate, LayoutCoordinate layoutCoordinate) {
        taskUpdateDelegateMap[layoutCoordinate.x, layoutCoordinate.y].Add(notificationDelegate);

        MasterGameTask associatedTask = associatedTasksCoordinateMap[layoutCoordinate.x, layoutCoordinate.y];
        Unit unit = null;

        if(associatedTask != null) {
            unit = associatedTask.assignedUnit;
        }

        notificationDelegate.NowPerformingTask(unit, associatedTask, null);
    }

    public void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate, LayoutCoordinate layoutCoordinate) {
        taskUpdateDelegateMap[layoutCoordinate.x, layoutCoordinate.y].Remove(notificationDelegate);
    }

    protected void NotifyAllTaskStatus(LayoutCoordinate coordinate) {
        foreach(TaskStatusUpdateDelegate updateDelegate in taskUpdateDelegateMap[coordinate.x, coordinate.y]) {
            MasterGameTask associatedTask = associatedTasksCoordinateMap[coordinate.x, coordinate.y];
            Unit unit = null;

            if (associatedTask != null) {
                unit = associatedTask.assignedUnit;
            }

            updateDelegate.NowPerformingTask(unit, associatedTask, null);
        }
    }

    /*
    * Invalid TaskStatusNotifiable Interface
    * */

    [System.Obsolete("Invalid for type Map", true)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        throw new System.InvalidOperationException();

    }

    [System.Obsolete("Invalid for type Map", true)]
    public override void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        throw new System.InvalidOperationException();
    }

    [System.Obsolete("Invalid for type Map", true)]
    protected override void NotifyAllTaskStatus() {
        throw new System.InvalidOperationException();
    }
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member


    /*
     * Map Version Of UserActionNotifiable Interface
     * */

    public void RegisterForUserActionNotifications(UserActionUpdateDelegate notificationDelegate, LayoutCoordinate layoutCoordinate) {
        userActionDelegateMap[layoutCoordinate.x, layoutCoordinate.y].Add(notificationDelegate);

        notificationDelegate.UpdateUserActionsAvailable(userActionCoordinateMap[layoutCoordinate.x, layoutCoordinate.y]);
    }

    public void EndUserActionNotifications(UserActionUpdateDelegate notificationDelegate, LayoutCoordinate layoutCoordinate) {
        userActionDelegateMap[layoutCoordinate.x, layoutCoordinate.y].Remove(notificationDelegate);
    }

    public void NotifyAllUserActionsUpdate(LayoutCoordinate layoutCoordinate) {
        foreach(UserActionUpdateDelegate updateDelegate in userActionDelegateMap[layoutCoordinate.x, layoutCoordinate.y]) {
            updateDelegate.UpdateUserActionsAvailable(userActionCoordinateMap[layoutCoordinate.x, layoutCoordinate.y]);
        }
    }
}
