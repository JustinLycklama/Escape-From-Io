using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;

public enum MineralType { Copper, Silver, Gold, RefinedCopper, RefinedSilver, RefinedGold, Azure }

public interface OreUpdateDelegate {
    void OreAdded(Ore ore);
    void OreRemoved(Ore ore);
}

public class GameResourceManager : MonoBehaviour {
    private int oreCount = 0;

    public Mesh[] mineralMeshList;

    public Sprite rawCopperImage;
    public Sprite rawSilverImage;
    public Sprite rawGoldImage;
    public Sprite azureImage;

    public Sprite refinedCopperImage;
    public Sprite refinedSilverImage;
    public Sprite refinedGoldImage;



    //public struct MineralTypeCount {
    //    public int count;
    //    public MineralType type;

    //    public MineralTypeCount(int count, MineralType type) : this() {
    //        this.count = count;
    //        this.type = type;
    //    }
    //}

    Dictionary<LayoutCoordinate, Dictionary<MineralType, int>> mineralsForLayoutCoordinate;

    private class Blueprint : PrefabBlueprint {
        private static string folder = "Ore/";

        public static Blueprint Basic = new Blueprint("Ore", typeof(Ore));
        public static Blueprint Silver = new Blueprint("Silver", typeof(Ore));
        public static Blueprint Gold = new Blueprint("Gold", typeof(Ore));
        public static Blueprint Azure = new Blueprint("Azure", typeof(Ore));

        private Blueprint(string fileName, Type type) : base(folder + fileName, type) { }
    }

    // List of all unclaimedOre
    List<Ore> availableOreList;

    // List of all ore currently on the map, both in a units possession or not
    public List<Ore> globalOreList;


    Dictionary<Refinery, Ore[]> refineryOreDistribution;
    Dictionary<Unit, List<Ore>> unitOreDistribution;

    private GameResourceManager() {

        availableOreList = new List<Ore>();

        refineryOreDistribution = new Dictionary<Refinery, Ore[]>();
        unitOreDistribution = new Dictionary<Unit, List<Ore>>();

        mineralsForLayoutCoordinate = new Dictionary<LayoutCoordinate, Dictionary<MineralType, int>>();
    }

    public bool AnyMineralAvailable(MineralType type) {
        return GetAllAvailableOfType(type).Count() > 0;
    }

    //Dictionary<TerrainType, int> terrainTypeCount = new Dictionary<TerrainType, int>();

    public void RegisterMapForMinerals(Map map) {
        Constants constants = Script.Get<Constants>();
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        System.Random rnd = NoiseGenerator.random;

        int sampleXOffset = constants.layoutMapWidth * map.mapContainer.mapX;
        int sampleYOffset = constants.layoutMapHeight * map.mapContainer.mapY;

        for(int y = 0; y < constants.layoutMapHeight; y++) {
            for(int x = 0; x < constants.layoutMapWidth; x++) {
                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, map.mapContainer);
                TerrainType terrainType = map.GetTerrainAt(layoutCoordinate);

                // Find what the terrain will be, if it is unknown
                if (terrainType.regionType == RegionType.Type.Unknown) {
                    terrainType = mapGenerator.KnownTerrainTypeAtIndex(x + sampleXOffset, y + sampleYOffset);
                }

                //terrainTypeCount[terrainType]++;

                //Dictionary<MineralType, int> maxIterationForMineral = new Dictionary<MineralType, int>() { MineralType.Copper :  }

                Dictionary<MineralType, int> mineralTypeCounts = new Dictionary<MineralType, int>();
                foreach(TerrainType.MineralChance mineralChance in terrainType.mineralChances) {
                    int mineralCount = 0;

                    for(int iteration = 0; iteration < mineralChance.maxNumberGenerated; iteration++) {
                        float coinToss = (rnd.Next(0, 100) / 100.0f);

                        if (TutorialManager.isTutorial) {
                            coinToss = 1.0f;
                        }

                        float threshold = (1 - mineralChance.chance.GetPercentage());

                        if(coinToss > threshold) {
                            mineralCount++;
                        }
                    }

                    mineralTypeCounts.Add(mineralChance.type, mineralCount);
                }

                mineralsForLayoutCoordinate[layoutCoordinate] = mineralTypeCounts;
            }
        }
    }

    public Dictionary<MineralType, int> MineralListForCoordinate(LayoutCoordinate layoutCoordinate) {
        if (mineralsForLayoutCoordinate.ContainsKey(layoutCoordinate)) {
            return mineralsForLayoutCoordinate[layoutCoordinate];
        }

        return new Dictionary<MineralType, int>(); 
    }

    public void ClearMineralsAtCoordinate(LayoutCoordinate layoutCoordinate) {
        if(mineralsForLayoutCoordinate.ContainsKey(layoutCoordinate)) {
            mineralsForLayoutCoordinate.Remove(layoutCoordinate);
        }
    }

    public Ore CreateMineral(MineralType type) {
        Ore ore = null;

        switch(type) {
            case MineralType.Copper:
                ore = Blueprint.Basic.Instantiate() as Ore;
                break;
            case MineralType.Silver:
                ore = Blueprint.Silver.Instantiate() as Ore;
                break;
            case MineralType.Gold:
                ore = Blueprint.Gold.Instantiate() as Ore;
                break;
            case MineralType.Azure:
                ore = Blueprint.Azure.Instantiate() as Ore;
                break;
        }

        ore.name = type.ToString() + " #" + oreCount;
        ore.mineralType = type;
        oreCount++;

        System.Random rnd = NoiseGenerator.random;
        int index = rnd.Next(0, mineralMeshList.Length);

        ore.GetComponentInChildren<MeshFilter>().mesh = mineralMeshList[index];

        ore.transform.SetParent(transform, true);

        availableOreList.Add(ore);
        NotifyAllOreUpdate(ore, true);

        globalOreList.Add(ore);

        return ore;
    }

    public Ore[] GetAllAvailableOfType(MineralType gatherType) {
        return availableOreList.Where(ore => ore.associatedTask == null && ore.taskAlreadyDictated == false && ore.mineralType == gatherType).ToArray();
    }

    public bool isHoldingResources(Unit unit) {
        if(!unitOreDistribution.ContainsKey(unit) || unitOreDistribution[unit].Count == 0) {
            return false;
        }

        return true;
    }

    public MineralType ConsumeInBuilding(Unit oreHolder, Building building) {

        if(!isHoldingResources(oreHolder)) {
            throw new Exception();
        }
        
        Ore anyOre = unitOreDistribution[oreHolder][0];

        MineralType mineralType = anyOre.mineralType;

        unitOreDistribution[oreHolder].Remove(anyOre);
        globalOreList.Remove(anyOre);

        DestroyImmediate(anyOre.gameObject);

        return mineralType;
    }
    
    public void GiveToUnit(Ore ore, Unit unit) {

        if (!availableOreList.Contains(ore)) {
            // This ore cannot be given
            return;
        }

        if(!unitOreDistribution.ContainsKey(unit)) {
            unitOreDistribution.Add(unit, new List<Ore>());
        }

        // WorldPositionStays false affects the ore scale
        ore.transform.SetParent(unit.oreLocation, true);
        ore.transform.localPosition = new Vector3(0, 0, 0);

        List<Ore> oreList = unitOreDistribution[unit];
        availableOreList.Remove(ore);
        NotifyAllOreUpdate(ore, false);

        oreList.Add(ore);
    }

    public void ReturnAllToEnvironment(Unit unit) {
        if (!unitOreDistribution.ContainsKey(unit)) {
            // This unit never had any ore
            return;
        }

        List<Ore> oreList = unitOreDistribution[unit];

        foreach(Ore ore in oreList.ToArray()) {
            ore.transform.SetParent(transform, true);

            oreList.Remove(ore);
            availableOreList.Add(ore);

            NotifyAllOreUpdate(ore, true);
        }        
    }

    private Dictionary<MineralType, int> tallyCountDictionary;
    public void CostPanelToEnvironmentDump(Dictionary<MineralType, int> tallyCountDictionary) {
        this.tallyCountDictionary = tallyCountDictionary;
    }

    public Dictionary<MineralType, int>  FloatingCostPanelResources() {
        Dictionary<MineralType, int> retDict = tallyCountDictionary ?? new Dictionary<MineralType, int>();

        tallyCountDictionary = null;

        return retDict;
    }

    /*
     * Task Queue Methods
     * */

    public void CueueGatherTasksForCost(BlueprintCost cost, WorldPosition depositPosition, ActionableItem actionableItem, bool asLastPriority = false) {

        TaskQueueManager queue = Script.Get<TaskQueueManager>();
        List<MasterGameTask> blockingBuildTasks = new List<MasterGameTask>();

        foreach(MineralType mineralType in cost.costMap.Keys) {

            GameTask oreTask = new GameTask("Pick Up", mineralType, GameTask.ActionType.PickUp, null);
            oreTask.SatisfiesStartRequirements = () => {
                return Script.Get<GameResourceManager>().AnyMineralAvailable(mineralType);
            };

            GameTask dropTask = new GameTask("Deposit", depositPosition, GameTask.ActionType.DropOff, actionableItem, PathRequestTargetType.PathGrid);

            string collectMasterTitle = "Collect " + mineralType.ToString() + " for " + actionableItem.description;
            MasterGameTask masterCollectTask = new MasterGameTask(MasterGameTask.ActionType.Move, collectMasterTitle, new GameTask[] { oreTask, dropTask }, null, asLastPriority);
            masterCollectTask.repeatCount = cost.costMap[mineralType];

            queue.QueueTask(masterCollectTask);
            blockingBuildTasks.Add(masterCollectTask);
        }

        GameTask buildTask = new GameTask(null, depositPosition, GameTask.ActionType.Build, actionableItem, PathRequestTargetType.PathGrid);
        MasterGameTask masterBuildTask = new MasterGameTask(MasterGameTask.ActionType.Build, "Build " + actionableItem.description, new GameTask[] { buildTask }, blockingBuildTasks, asLastPriority);

        masterBuildTask.itemContingentOnTask = actionableItem;

        queue.QueueTask(masterBuildTask);

        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(depositPosition);
        LayoutCoordinate coordinate = new LayoutCoordinate(mapCoordinate);

        coordinate.mapContainer.map.AssociateTask(masterBuildTask, coordinate);
    }


    /*
     * OreUpdateDelegate Methods
     * */

    public List<OreUpdateDelegate> oreUpdateDelegateList = new List<OreUpdateDelegate>();

    public void RegisterFoOreNotifications(OreUpdateDelegate notificationDelegate) {
        oreUpdateDelegateList.Add(notificationDelegate);
    }

    public void EndOreNotifications(OreUpdateDelegate notificationDelegate) {
        oreUpdateDelegateList.Remove(notificationDelegate);
    }

    public void NotifyAllOreUpdate(Ore ore, bool isAdded) {
        foreach(OreUpdateDelegate updateDelegate in oreUpdateDelegateList) {
            if (isAdded) {
                updateDelegate.OreAdded(ore);
            } else {
                updateDelegate.OreRemoved(ore);
            }
            
        }
    }
}
