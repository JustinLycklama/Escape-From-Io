using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainManager : MonoBehaviour {

    // Used to determine terrain at region
    float[,] groundMutatorMap;
    float[,] mountainMutatorMap;

    public RegionType[] regionTypes;

    public TerrainType[] waterTerrainTypes;
    public TerrainType[] landTerrainTypes;
    public TerrainType[] mountainTerrainTypes;

    [HideInInspector]
    public TerrainType[] terrainTypes {
        get {
            return waterTerrainTypes.Concat(landTerrainTypes).Concat(mountainTerrainTypes).ToArray();
        }
    }

    [HideInInspector]
    public Dictionary<RegionType.Type, RegionType> regionTypeMap;

    [HideInInspector]
    public Dictionary<RegionType, List<TerrainType>> regionToTerrainTypeMap;

    [HideInInspector]
    public Dictionary<TerrainType.Type, TerrainType> terrainTypeMap;

    private void Awake() {
        MapGenerator mapGen = Script.Get<MapGenerator>();

        regionTypeMap = new Dictionary<RegionType.Type, RegionType>();
        regionToTerrainTypeMap = new Dictionary<RegionType, List<TerrainType>>();
        terrainTypeMap = new Dictionary<TerrainType.Type, TerrainType>();

        foreach(RegionType regionType in regionTypes) {
            regionTypeMap[regionType.type] = regionType;
            regionToTerrainTypeMap[regionType] = new List<TerrainType>();
        }

        for(int i = 0; i < waterTerrainTypes.Length; i++) {
            waterTerrainTypes[i].regionType = RegionType.Type.Water;
        }

        for(int i = 0; i < landTerrainTypes.Length; i++) {
            landTerrainTypes[i].regionType = RegionType.Type.Land;
        }

        for(int i = 0; i < mountainTerrainTypes.Length; i++) {
            mountainTerrainTypes[i].regionType = RegionType.Type.Mountain;
        }

        foreach(TerrainType terrainType in terrainTypes) {
            terrainTypeMap[terrainType.type] = terrainType;
            regionToTerrainTypeMap[regionTypeMap[terrainType.regionType]].Add(terrainType);
        }
    }

    public void SetGroundMutatorMap(float[,] groundMutatorMap) {
        this.groundMutatorMap = groundMutatorMap;
    }

    public void SetMounainMutatorMap(float[,] mountainMutatorMap) {
        this.mountainMutatorMap = mountainMutatorMap;
    }

    public RegionType RegionTypeForValue(float value) {
        foreach (RegionType type in regionTypes) {
            if (value <= type.noiseMax && value >= type.noiseBase) {
                return type;
            }
        }

        return regionTypeMap[RegionType.Type.Water];
    }

    public TerrainType TerrainTypeForRegion(RegionType regionType, int x, int y) {
        float mutatorValue = 0;

        switch(regionType.type) {
            case RegionType.Type.Land:
                mutatorValue = groundMutatorMap[x, y];
                break;
            case RegionType.Type.Mountain:
                mutatorValue = mountainMutatorMap[x, y];
                break;
            default:
                break;
        }

        List<TerrainType> terrainsForRegion = regionToTerrainTypeMap[regionType];
        foreach(TerrainType terrainType in terrainsForRegion) {
            if(mutatorValue <= terrainType.mutatorNoiseMax && mutatorValue >= terrainType.mutatorNoiseBase) {
                return terrainType;
            }
        }

        return terrainTypeMap[TerrainType.Type.Water];
    }

    public TerrainType? CanTerriformTo(TerrainType terrainType) {
        if (terrainType.regionType == RegionType.Type.Mountain) {
            return terrainTypeMap[TerrainType.Type.Mud];
        }

        if(terrainType.type == TerrainType.Type.Mud || terrainType.type == TerrainType.Type.Grass) {
            return terrainTypeMap[TerrainType.Type.Sand];
        }

        return null;
    }

    public UserAction[] ActionsAvailableForTerrain(LayoutCoordinate coordinate) {

        Map map = coordinate.mapContainer.map;

        TerrainType terrainType = map.GetTerrainAt(coordinate);

        List<UserAction> actionList = new List<UserAction>();

        TerrainType? terraformable = CanTerriformTo(terrainType);

        switch(terrainType.regionType) {
            case RegionType.Type.Land:

                if (terraformable != null) {
                    UserAction action = new UserAction();

                    action.description = "Clean";
                    action.layoutCoordinate = coordinate;

                    action.performAction = (LayoutCoordinate layoutCoordinate) => {
                        TaskQueueManager queue = Script.Get<TaskQueueManager>();

                        MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
                        WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                        GameTask cleaningTask = new GameTask("Cleaning", worldPosition, GameTask.ActionType.FlattenPath, layoutCoordinate.mapContainer.map, PathRequestTargetType.Layout);
                        MasterGameTask masterCleaningTask = new MasterGameTask(MasterGameTask.ActionType.Mine, "Clean location " + layoutCoordinate.description, new GameTask[] { cleaningTask });

                        queue.QueueTask(masterCleaningTask);
                        map.AssociateTask(masterCleaningTask, layoutCoordinate);
                    };

                    actionList.Add(action);
                }

                if (terrainType.buildable) {
                    UserAction action = new UserAction();
                    action.description = "Building";
                    action.layoutCoordinate = coordinate;

                    action.blueprintList = new ConstructionBlueprint[] { Building.Blueprint.Tower, Building.Blueprint.Refinery };

                    actionList.Add(action);
                }

                break;
            case RegionType.Type.Mountain:

                if(terraformable != null) {
                    UserAction action = new UserAction();

                    action.description = "Mine Wall";
                    action.layoutCoordinate = coordinate;

                    action.performAction = (LayoutCoordinate layoutCoordinate) => {
                        TaskQueueManager queue = Script.Get<TaskQueueManager>();

                        MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
                        WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                        GameTask miningTask = new GameTask("Mining", worldPosition, GameTask.ActionType.Mine, layoutCoordinate.mapContainer.map, PathRequestTargetType.Layout);
                        MasterGameTask masterMiningTask = new MasterGameTask(MasterGameTask.ActionType.Mine, "Mine at location " + layoutCoordinate.description, new GameTask[] { miningTask });

                        queue.QueueTask(masterMiningTask);
                        map.AssociateTask(masterMiningTask, layoutCoordinate);
                    };

                    actionList.Add(action);
                }
                break;
            default:
                break;
        }

        return actionList.ToArray();
    }
}

[System.Serializable]
public struct RegionType {
    public enum Type { Water, Land, Mountain }
    public Type type;

    public float noiseBase;
    public float noiseMax;

    public bool plateauAtBase;

    public Color color;

    [HideInInspector]
    public int priority {
        get {
            return type.Priority();
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;

    public enum Type { Water, Sand, Grass, Mud, LooseRock, Rock, HardRock }
    public Type type;

    [HideInInspector]
    public RegionType.Type regionType;

    public float mutatorNoiseBase;
    public float mutatorNoiseMax;

    public bool walkable;
    public bool buildable;
    
    [Range(0, 1)]
    public float walkSpeedMultiplier;
    [Range(0, 1)]
    public float modificationSpeedModifier;

    public Texture2D texture;
    public float textureScale;

    [HideInInspector]
    public int priority {
        get {
            return regionType.Priority();
        }
    }

    [HideInInspector]
    public Color color {
        get {
            TerrainManager manager = Script.Get<TerrainManager>();
            return manager.regionTypeMap[regionType].color;
        }
    }

    public override bool Equals(object obj) {
        return base.Equals(obj);
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }

    public override string ToString() {
        return regionType.ToString();
    }

    public static bool operator ==(TerrainType obj1, TerrainType obj2) {
        return obj1.type == obj2.type;
    }

    public static bool operator !=(TerrainType obj1, TerrainType obj2) {
        return !(obj1 == obj2);
    }
}

static class EnumExtensions {
    public static int Priority(this RegionType.Type type) {
        switch(type) {
            case RegionType.Type.Water:
                return 0;
            case RegionType.Type.Land:
                return 1;
            case RegionType.Type.Mountain:
                return 2;
            default:
                return 0;
        }
    }
}
