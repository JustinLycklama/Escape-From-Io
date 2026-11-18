using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerraformTarget {
    public LayoutCoordinate coordinate;
    public TerrainType terrainTypeTarget;

    public float[,] heightTarget;
    public float[,] initialHeight;

    // Edge samples of the 4 neighbouring tiles that re-shape because this tile changes (e.g. a mountain beside a mined-out
    // tile ramps down to it). Global MapCoordinate samples, animated with the same percentage (#29). Filled by MapGenerator.
    public List<Vector2Int> neighbourSamples;
    public List<float> neighbourInitialHeight;
    public List<float> neighbourHeightTarget;
    public List<Vector2Int> neighbourTiles;   // global layout coordinates of neighbours with changed samples

    public float percentage;

    public TerraformTarget(LayoutCoordinate coordinate, TerrainType terrainTypeTarget) {
        this.coordinate = coordinate;
        this.terrainTypeTarget = terrainTypeTarget;

        percentage = 0;
    }
}

public class Map : ActionableItem, MasterTaskUpdateDelegate {

    // Map Width + Height in Map Coordinates (Not Layout Coordinates)
    public int mapWidth;
    public int mapHeight;

    public MeshData meshData;
    public Texture2D meshTexture;

    public int featuresPerLayoutPerAxis;

    public MapContainer mapContainer;

    TerraformTarget[,] terraformTargetCoordinateMap;

    public override string description => "Terrain";

    public void InitMap(TerrainType[,] terrainData, MeshData meshData) { //  float[,] finalHeightMap, float[,] layoutNoiseMap, float[,] groundFeaturesNoiseMap, float[,] mountainFeaturesNoiseMap,
        Constants constants = Script.Get<Constants>();
        featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;


        // Replacement of below
        mapWidth = terrainData.GetLength(0) * constants.featuresPerLayoutPerAxis;
        mapHeight = terrainData.GetLength(1) * constants.featuresPerLayoutPerAxis;


        this.meshData = meshData;

        int terrainWidth = terrainData.GetLength(0);
        int terrainHeight = terrainData.GetLength(1);

        terraformTargetCoordinateMap = new TerraformTarget[terrainWidth, terrainHeight];
    }

    public TerrainType GetTerrainAt(LayoutCoordinate layoutCoordinate) {
        return Script.Get<MapGenerator>().GetTerrainAt(layoutCoordinate);
    }

    // Returns the height in MAP COORDINATE position
    public float getHeightAt(MapCoordinate coordinate) {

        //// If I have a map coordinate, should it be guarenteed to be on the map?

        //// TODO: Triangle Calculations        

        return Script.Get<MapGenerator>().GetHeightAt(coordinate);        
    }

    public void UpdateTerrainAtLocation(LayoutCoordinate layoutCoordinate, TerrainType terrainType) {
        // Update the terrain type at this location
        Script.Get<MapGenerator>().UpdateTerrainAt(layoutCoordinate, terrainType);

        // Update the BoxCollider Height
        mapContainer.ResizeBoxColliderAt(layoutCoordinate);

        // Recalculate Sight
        Script.Get<BuildingManager>().RecalcluateSightStatuses();

        // Update pathfinding grid
        Script.Get<PathfindingGrid>().UpdateGrid(this, layoutCoordinate);

        UpdateUserActionsAt(layoutCoordinate);
        mapContainer.UpdateShaderTerrainTextures();

        TutorialManager.sharedInstance.Fire(TutorialTrigger.TerraformComplete);
    }

    /*
     * Actionable Item Override Properties
     * */

    UserAction[,][] userActionCoordinateMap;
    List<UserActionUpdateDelegate>[,] userActionDelegateMap;

    public MasterGameTask[,] associatedTasksCoordinateMap { get; private set; }
    private List<TaskStatusUpdateDelegate>[,] taskUpdateDelegateMap;

    public void CreateAllActionableItemOverrides() {
        Constants constants = Script.Get<Constants>();

        int width = constants.layoutMapWidth;
        int height = constants.layoutMapHeight;

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

    public void UpdateUserActionsAt(LayoutCoordinate layoutCoordinate) {
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
            action.shouldDeselectAfterAction = false;

            action.performAction = (LayoutCoordinate layoutCoordinate) => {
                associatedTasksCoordinateMap[layoutCoordinate.x, layoutCoordinate.y].CancelTask();

                mapContainer.NotifyUpdateActionAt(layoutCoordinate);

                GameResourceManager resourceManager = Script.Get<GameResourceManager>();
                PlaceMineralsAroundLocation(new WorldPosition(new MapCoordinate(layoutCoordinate)), resourceManager.FloatingCostPanelResources());
            };

            actionList.Add(action);
        } else {
            return terrainManager.ActionsAvailableForTerrain(coordinate);


            // Build?
        }


        //// Mine?

        return actionList.ToArray();
    }

    /*
     * Actionable Item Interface
     * */
    public override float performAction(GameTask task, float rate, Unit unit) {

        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(task.target);
        LayoutCoordinate coordinate = new LayoutCoordinate(mapCoordinate);

        return TerraformAtLocation(coordinate, rate, true);
    }

    public float TerraformAtLocation(LayoutCoordinate coordinate, float rateOrPercent, bool isRate) {
        TerraformTarget terraformTarget;

        if(terraformTargetCoordinateMap[coordinate.x, coordinate.y] != null) {
            terraformTarget = terraformTargetCoordinateMap[coordinate.x, coordinate.y];
        } else {
            TerrainManager terrainManager = Script.Get<TerrainManager>();
            TerrainType originalTerrain = GetTerrainAt(coordinate);

            TerrainType? targetTerrain = terrainManager.CanTerriformTo(originalTerrain);

            if(targetTerrain == null) {
                // Do not attempt to terraform a target point into the same terrain type
                return 1;
            }

            RegionType regionType = terrainManager.regionTypeMap[targetTerrain.Value.regionType];
            float targetTerrainHeight = regionType.plateauAtBase ? regionType.noiseBase : regionType.noiseMax;

            terraformTarget = new TerraformTarget(coordinate, targetTerrain.Value);
            terraformTargetCoordinateMap[coordinate.x, coordinate.y] = terraformTarget;
        }

        float previousPercentage = terraformTarget.percentage;

        if (isRate) {
            terraformTarget.percentage += rateOrPercent * GetTerrainAt(coordinate).modificationSpeedModifier;
        } else {
            terraformTarget.percentage = rateOrPercent;
        }

        if(terraformTarget.percentage >= 1) {
            terraformTarget.percentage = 1;

            // Terraform complete

            UpdateTerrainAtLocation(terraformTarget.coordinate, terraformTarget.terrainTypeTarget);
            TerraformHeightMap(terraformTarget);

            terraformTargetCoordinateMap[coordinate.x, coordinate.y] = null;

            // Create Ore at location
            PlaceMineralsAtLocation(coordinate);

        } else {
            if(Mathf.RoundToInt(previousPercentage * 25) != Mathf.RoundToInt(terraformTarget.percentage * 25)) {
                if(GetTerrainAt(coordinate).regionType != terraformTarget.terrainTypeTarget.regionType) {
                    TerraformHeightMap(terraformTarget);
                }
            }
        }

        return terraformTarget.percentage;
    }

    public void PlaceMineralsAtLocation(LayoutCoordinate layoutCoordinate) {
        GameResourceManager resourceManager = Script.Get<GameResourceManager>();
        Dictionary<MineralType, int> mineralLists = resourceManager.MineralListForCoordinate(layoutCoordinate);

        PlaceMineralsAroundLocation(new WorldPosition(new MapCoordinate(layoutCoordinate)), mineralLists);

        resourceManager.ClearMineralsAtCoordinate(layoutCoordinate);
    }

    public void TerraformHeightMap(TerraformTarget terraformTarget, bool delayMeshUpdate = false) {
        MapGenerator mapGenerator = Script.Get<MapGenerator>();
        float[,] finalHeightMap = mapGenerator.TerraformHeightMap(terraformTarget);

        MeshGenerator.UpdateTerrainMesh(meshData, finalHeightMap, featuresPerLayoutPerAxis, terraformTarget.coordinate);

        if (!delayMeshUpdate) {
            mapContainer.DrawMesh();
            mapContainer.UpdateNeibourOverhangsForCoordinate(terraformTarget.coordinate);
            UpdateNeighbourTileMeshes(terraformTarget);
        }
    }

    // Re-mesh the neighbouring tiles whose edges re-shaped with this terraform (#29). They can be in other containers.
    private void UpdateNeighbourTileMeshes(TerraformTarget terraformTarget) {
        if (terraformTarget.neighbourTiles == null || terraformTarget.neighbourTiles.Count == 0) {
            return;
        }

        Constants constants = Script.Get<Constants>();
        MapGenerator mapGenerator = Script.Get<MapGenerator>();
        MapsManager mapsManager = Script.Get<MapsManager>();

        foreach (Vector2Int tile in terraformTarget.neighbourTiles) {
            MapContainer container = mapsManager.mapContainers.Find(m => m.mapX == tile.x / constants.layoutMapWidth && m.mapY == tile.y / constants.layoutMapHeight);
            if (container == null || container.map == null) {
                continue;
            }

            LayoutCoordinate local = new LayoutCoordinate(tile.x % constants.layoutMapWidth, tile.y % constants.layoutMapHeight, container);

            // The subset array is pooled, so consume it before the next call
            float[,] containerHeights = mapGenerator.FinalHeightSubsetFor(container);
            MeshGenerator.UpdateTerrainMesh(container.map.meshData, containerHeights, featuresPerLayoutPerAxis, local);

            container.DrawMesh();
            container.UpdateNeibourOverhangsForCoordinate(local);
        }
    }

    private void PlaceMineralsAroundLocation(WorldPosition worldPosition, Dictionary<MineralType, int> mineralLists) {
        float offset = 25;

        Vector3 position = worldPosition.vector3;
        GameResourceManager resourceManager = Script.Get<GameResourceManager>();

        Vector3 position1 = position + new Vector3(offset, 0, 0);
        Vector3 position2 = position + new Vector3(0, 0, offset);
        Vector3 position3 = position + new Vector3(-offset, 0, 0);
        Vector3 position4 = position + new Vector3(0, 0, -offset);

        Vector3 position5 = position + new Vector3(offset, 0, offset);
        Vector3 position6 = position + new Vector3(offset, 0, -offset);
        Vector3 position7 = position + new Vector3(-offset, 0, offset);
        Vector3 position8 = position + new Vector3(-offset, 0, -offset);

        Vector3 position9 = position + new Vector3(0, 0, 0);

        List<Vector3> positions = new List<Vector3> { position1, position2, position3, position4, position5, position6, position7, position8, position9 };

        foreach(MineralType mineralType in mineralLists.Keys) {
            for(int i = 0; i < mineralLists[mineralType]; i++) {
                int rInt = Random.Range(0, positions.Count - 1);

                Vector3 randomPosition = positions[rInt];
                positions.RemoveAt(rInt);

                Ore ore = resourceManager.CreateMineral(mineralType);
                ore.transform.position = randomPosition;
            }
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

    Dictionary<MasterGameTask, LayoutCoordinate> gameTaskLocationMap = new Dictionary<MasterGameTask, LayoutCoordinate>();

    public void AssociateTask(MasterGameTask task, LayoutCoordinate coordinate) {

        MasterGameTask oldTask = associatedTasksCoordinateMap[coordinate.x, coordinate.y];
        if (oldTask != null) {
            oldTask.EndTaskStatusNotifications(this);
            gameTaskLocationMap.Remove(oldTask);
        }

        associatedTasksCoordinateMap[coordinate.x, coordinate.y] = task;

        if(task != null) {
            task.RegisterForTaskStatusNotifications(this);
            gameTaskLocationMap[task] = coordinate;
        }      

        UpdateUserActionsAt(coordinate);
        NotifyAllTaskStatus(coordinate);
        mapContainer.NotifyUpdateActionAt(coordinate);
    }

    /*
     * MasterTaskUpdateDelegate Interface
     * */

    public void RepeatCountUpdated(MasterGameTask masterGameTask, int count) { }

    public void TaskCancelled(MasterGameTask masterGameTask) {
        if (gameTaskLocationMap.ContainsKey(masterGameTask)) {
            AssociateTask(null, gameTaskLocationMap[masterGameTask]);
        }
    }

    public void TaskFinished(MasterGameTask masterGameTask) {
        if(gameTaskLocationMap.ContainsKey(masterGameTask)) {
            AssociateTask(null, gameTaskLocationMap[masterGameTask]);
        }
    }

    public void TaskBlockerRemoved(MasterGameTask masterGameTask) { }
    public void TaskUnitAssigned(MasterGameTask masterGameTask) { }

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
