using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;

public enum MineralType { Copper, Silver, Gold, RefinedCopper, RefinedSilver, RefinedGold }

public interface OreUpdateDelegate {
    void NewOreCreated(Ore ore);
    void OreRemoved(Ore ore);
}

public class GameResourceManager : MonoBehaviour {
    private int oreCount = 0;

    public Mesh[] mineralMeshList;

    public Sprite rawCopperImage;
    public Sprite rawSilverImage;
    public Sprite rawGoldImage;

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

        System.Random rnd = new System.Random();

        for(int y = 0; y < constants.layoutMapHeight; y++) {
            for(int x = 0; x < constants.layoutMapWidth; x++) {
                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, map.mapContainer);
                TerrainType terrainType = map.GetTerrainAt(layoutCoordinate);

                //terrainTypeCount[terrainType]++;

                Dictionary<MineralType, int> mineralTypeCounts = new Dictionary<MineralType, int>();
                foreach(TerrainType.MineralChance mineralChance in terrainType.mineralChances) {
                    float coinToss = (rnd.Next(0, 100) / 100.0f);
                    float threshold = (1 - mineralChance.chance.GetPercentage());

                    if (coinToss > threshold) {
                        mineralTypeCounts.Add(mineralChance.type, 1);
                    } 
                }

                mineralsForLayoutCoordinate[layoutCoordinate] = mineralTypeCounts;
            }
        }
    }

    public void CompleteMineralRegistration() {

    }

    public Dictionary<MineralType, int> MineralListForCoordinate(LayoutCoordinate layoutCoordinate) {
        if (mineralsForLayoutCoordinate.ContainsKey(layoutCoordinate)) {
            return mineralsForLayoutCoordinate[layoutCoordinate];
        }

        return new Dictionary<MineralType, int>(); 
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
        }

        ore.name = type.ToString() + " #" + oreCount;
        ore.mineralType = type;
        oreCount++;

        System.Random rnd = new System.Random(Guid.NewGuid().GetHashCode());
        int index = rnd.Next(0, mineralMeshList.Length);

        ore.GetComponentInChildren<MeshFilter>().mesh = mineralMeshList[index];

        ore.transform.SetParent(transform, true);

        availableOreList.Add(ore);
        globalOreList.Add(ore);

        return ore;
    }

    public Ore[] GetAllAvailableOfType(MineralType gatherType) {
        return availableOreList.Where(ore => ore.associatedTask == null && ore.taskAlreadyDictated == false && ore.mineralType == gatherType).ToArray();
    }

    //public void AddOreToStorage(Ore ore, Refinery refinery) {

    //}

    //public Ore TakeOreFromStorage(Refinery refinery) {

    //}

    public MineralType ConsumeInBuilding(Unit oreHolder, Building building) {

        if(!unitOreDistribution.ContainsKey(oreHolder) || unitOreDistribution[oreHolder].Count == 0) {
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

        ore.transform.SetParent(unit.transform, true);

        List<Ore> oreList = unitOreDistribution[unit];
        availableOreList.Remove(ore);
        oreList.Add(ore);
    }

    public List<OreUpdateDelegate> oreUpdateDelegateList = new List<OreUpdateDelegate>();

    public void RegisterFoOreNotifications(OreUpdateDelegate notificationDelegate) {
        oreUpdateDelegateList.Add(notificationDelegate);
    }

    public void EndOreNotifications(OreUpdateDelegate notificationDelegate) {
        oreUpdateDelegateList.Remove(notificationDelegate);
    }

    public void NotifyAllOreUpdate(Ore ore, bool isNew) {
        foreach(OreUpdateDelegate updateDelegate in oreUpdateDelegateList) {
            if (isNew) {
                updateDelegate.NewOreCreated(ore);
            } else {
                updateDelegate.OreRemoved(ore);
            }
            
        }
    }
}
