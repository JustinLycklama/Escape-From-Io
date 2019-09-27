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

    private TerrainType[] cachedTypes;
    [HideInInspector]
    public TerrainType[] terrainTypes {
        get {
            if (cachedTypes == null) {
                cachedTypes = waterTerrainTypes.Concat(landTerrainTypes).Concat(mountainTerrainTypes).ToArray();
            }

            return cachedTypes;
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

    public TerrainType TerrainTypeForRegion(RegionType regionType, int x, int y, out float mutatorOut) {
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

        mutatorOut = mutatorValue;

        List<TerrainType> terrainsForRegion = regionToTerrainTypeMap[regionType];
        foreach(TerrainType terrainType in terrainsForRegion) {
            if(mutatorValue < terrainType.mutatorNoiseMax && mutatorValue >= terrainType.mutatorNoiseBase) {
                return terrainType;
            }
        }

        return terrainTypeMap[TerrainType.Type.Water];
    }

    public TerrainType? CanTerriformTo(TerrainType terrainType) {
        if(terrainType.regionType == RegionType.Type.Water) {
            return terrainTypeMap[TerrainType.Type.Mud];
        }

        if (terrainType.regionType == RegionType.Type.Mountain && terrainType.type != TerrainType.Type.SolidRock) {
            return terrainTypeMap[TerrainType.Type.Mud];
        }

        if(terrainType.type == TerrainType.Type.Mud || terrainType.type == TerrainType.Type.Grass || terrainType.type == TerrainType.Type.Sand) {
            return terrainTypeMap[TerrainType.Type.Empty];
        }

        if (terrainType.type == TerrainType.Type.Empty) {
            return terrainTypeMap[TerrainType.Type.Path];
        }

        return null;
    }

    public UserAction[] ActionsAvailableForTerrain(LayoutCoordinate coordinate) {

        Map map = coordinate.mapContainer.map;

        TerrainType terrainType = map.GetTerrainAt(coordinate);

        List<UserAction> actionList = new List<UserAction>();

        TerrainType? terraformable = CanTerriformTo(terrainType);
    
        switch(terrainType.regionType) {
            case RegionType.Type.Water:
                /*if(terraformable.Value.type == TerrainType.Type.Mud) {

                    UserAction action = new UserAction();

                    action.description = "Terraform Land";
                    action.layoutCoordinate = coordinate;

                    action.performAction = (LayoutCoordinate layoutCoordinate) => {
                        Building.Blueprint.PathBuilding.ConstructAt(layoutCoordinate);
                    };

                    actionList.Add(action);
                }*/

                break;
            case RegionType.Type.Land:

                if (terraformable != null) {
                    if (terraformable.Value.type == TerrainType.Type.Empty) {

                        UserAction action = new UserAction();

                        action.description = "Clean " + coordinate.mapContainer.map.GetTerrainAt(coordinate).name;
                        action.layoutCoordinate = coordinate;

                        action.performAction = (LayoutCoordinate layoutCoordinate) => {
                            TaskQueueManager queue = Script.Get<TaskQueueManager>();
                            Map layoutCoordinateMap = layoutCoordinate.mapContainer.map;

                            MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
                            WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                            GameTask cleaningTask = new GameTask("Cleaning", worldPosition, GameTask.ActionType.FlattenPath, layoutCoordinate.mapContainer.map, PathRequestTargetType.PathGrid);
                            MasterGameTask masterCleaningTask = new MasterGameTask(MasterGameTask.ActionType.Build, "Clean " + layoutCoordinateMap.GetTerrainAt(layoutCoordinate).name, new GameTask[] { cleaningTask });

                            queue.QueueTask(masterCleaningTask);
                            map.AssociateTask(masterCleaningTask, layoutCoordinate);
                        };

                        actionList.Add(action);

                    } else if (terraformable.Value.type == TerrainType.Type.Path) {

                        UserAction action = new UserAction();

                        action.description = "Create Path";
                        action.layoutCoordinate = coordinate;

                        action.performAction = (LayoutCoordinate layoutCoordinate) => {
                            Building.Blueprint.PathBuilding.ConstructAt(layoutCoordinate);
                        };

                        actionList.Add(action);
                    }
                }

                Building buildingAtLocation = Script.Get<BuildingManager>().buildlingAtLocation(coordinate);
                if (terrainType.buildable && (buildingAtLocation == null || buildingAtLocation.GetType() == typeof(Path) || buildingAtLocation.GetType() == typeof(ShipProps))) {
                    UserAction unitAction = new UserAction();
                    unitAction.description = "Build Unit";
                    unitAction.layoutCoordinate = coordinate;

                    unitAction.blueprintList = new ConstructionBlueprint[] {
                        Unit.Blueprint.Miner, Unit.Blueprint.Mover, Unit.Blueprint.Builder,
                        Unit.Blueprint.AdvancedMiner, Unit.Blueprint.AdvancedMover, Unit.Blueprint.AdvancedBuilder
                    };

                    actionList.Add(unitAction);

                    UserAction buildingAction = new UserAction();
                    buildingAction.description = "Build Building";
                    buildingAction.layoutCoordinate = coordinate;

                    buildingAction.blueprintList = new ConstructionBlueprint[] { Building.Blueprint.Tower, Building.Blueprint.SensorTower, Building.Blueprint.AdvUnitBuilding }; //Building.Blueprint.Refinery,

                    actionList.Add(buildingAction);

                    UserAction shipAction = new UserAction();
                    shipAction.description = "Build Ship Parts";
                    shipAction.layoutCoordinate = coordinate;

                    shipAction.blueprintList = new ConstructionBlueprint[] { Building.Blueprint.StationShipFrame, Building.Blueprint.Thrusters, Building.Blueprint.Reactor, Building.Blueprint.Machining, Building.Blueprint.Telemerty, Building.Blueprint.StationShip }; //Building.Blueprint.Refinery,

                    actionList.Add(shipAction);
                }

                break;
            case RegionType.Type.Mountain:

                if(terraformable != null) {
                    UserAction action = new UserAction();

                    action.description = "Mine " + coordinate.mapContainer.map.GetTerrainAt(coordinate).name;
                    action.layoutCoordinate = coordinate;

                    action.performAction = (LayoutCoordinate layoutCoordinate) => {
                        TaskQueueManager queue = Script.Get<TaskQueueManager>();
                        Map layoutCoordinateMap = layoutCoordinate.mapContainer.map;

                        MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
                        WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                        GameTask miningTask = new GameTask("Mining", worldPosition, GameTask.ActionType.Mine, layoutCoordinateMap, PathRequestTargetType.Layout);
                        MasterGameTask masterMiningTask = new MasterGameTask(MasterGameTask.ActionType.Mine, "Mine " + layoutCoordinateMap.GetTerrainAt(layoutCoordinate).name, new GameTask[] { miningTask });

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
public enum Chance {
    Impossible, AlmostImpossible, Abysmal, Low, Medium, High, VeryHigh, AlmostGuarenteed, Guarenteed
}

public struct ChanceTier {
    public Vector2 valueRange;
    public float estimatedValue;

    public ChanceTier(Vector2 valueRange, float estimatedValue) {
        this.valueRange = valueRange;
        this.estimatedValue = estimatedValue;
    }
}


public class ChanceFactory {

    public static ChanceFactory shardInstance = new ChanceFactory();

    public Dictionary<Chance, ChanceTier> tierMap = new Dictionary<Chance, ChanceTier>();

    private ChanceFactory() {        
        List<Chance> chanceList = System.Enum.GetValues(typeof(Chance)).Cast<Chance>().ToList();

        chanceList.Remove(Chance.Impossible);
        chanceList.Remove(Chance.Guarenteed);

        float interval = 1f / chanceList.Count;
        float baseline = 0;

        for(int i = 0; i < chanceList.Count; i++) {
            float tierCeil = baseline + interval;
            tierMap.Add(chanceList[i], new ChanceTier(new Vector2(baseline, tierCeil), baseline + (interval / 2f)));

            baseline = tierCeil;
        }
    }

    public Chance ChanceFromPercent(float percent) {
        if (percent == 0) {
            return Chance.Impossible;
        } else if(percent == 1) {
            return Chance.Guarenteed;
        }

        foreach(Chance chance in tierMap.Keys) {
            ChanceTier tier = tierMap[chance];
            if (percent >= tier.valueRange.x && percent < tier.valueRange.y) {
                return chance;
            }
        }

        return Chance.VeryHigh;
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;

    public enum Type { Water, Empty, Grass, Mud, Path, ScorchedEarth, LooseRock, Rock, HardRock, SolidRock, AlunarRock, Sand }
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

    [System.Serializable]
    public struct MineralChance {
        public MineralType type;
        public Chance chance;
        public int maxNumberGenerated;
    }

    public MineralChance[] mineralChances;

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

    public static float GetPercentage(this Chance chance) {
        if(chance == Chance.Impossible) {
            return 0;
        } else if(chance == Chance.Guarenteed) {
            return 1;
        }

        return ChanceFactory.shardInstance.tierMap[chance].estimatedValue;
    }

    public static string NameAsRarity(this Chance chance) {
        switch(chance) {
            case Chance.Impossible:
                return "Never";
            case Chance.AlmostImpossible:
                return "Almost Never";
            case Chance.Abysmal:
                return "Very Rare";
            case Chance.Low:
                return "Rare";
            case Chance.Medium:
                return "Common";
            case Chance.High:
                return "Very Common";
            case Chance.VeryHigh:
                return "Plentiful";
            case Chance.AlmostGuarenteed:
                return "Bountiful";
            case Chance.Guarenteed:
                return "Always";
        }

        return "";
    }

    public static string NameAsDifficulty(this Chance chance) {
        switch(chance) {
            case Chance.Impossible:
                return "No Resistance";
            case Chance.AlmostImpossible:
                return "Easiest";
            case Chance.Abysmal:
                return "Very Easy";
            case Chance.Low:
                return "Easy";
            case Chance.Medium:
                return "Mediocre";
            case Chance.High:
                return "Difficult";
            case Chance.VeryHigh:
                return "Very Difficult";
            case Chance.AlmostGuarenteed:
                return "Insanely Difficult";
            case Chance.Guarenteed:
                return "Impossible";
        }

        return "";
    }

    public static string NameAsSkill(this Chance chance) {
        switch(chance) {
            case Chance.Impossible:
                return "Impossible";
            case Chance.AlmostImpossible:
                return "Glacier Speed";
            case Chance.Abysmal:
                return "Very Slow";
            case Chance.Low:
                return "Slow";
            case Chance.Medium:
                return "Decent";
            case Chance.High:
                return "Quick";
            case Chance.VeryHigh:
                return "Fast";
            case Chance.AlmostGuarenteed:
                return "Very Fast";
            case Chance.Guarenteed:
                return "Lightning";
        }

        return "";
    }
}
