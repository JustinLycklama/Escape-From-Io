using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public enum MineralType { Ore, Silver, Gold }

public class GameResourceManager : MonoBehaviour {
    private int oreCount = 0;



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

        private Blueprint(string fileName, Type type) : base(folder + fileName, type) { }
    }

    List<Ore> globalOreList;

    Dictionary<Refinery, Ore[]> refineryOreDistribution;
    Dictionary<Unit, List<Ore>> unitOreDistribution;

    private GameResourceManager() {

        globalOreList = new List<Ore>();

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
        Ore ore = Blueprint.Basic.Instantiate() as Ore;
        ore.name = "Ore #" + oreCount;
        oreCount++;

        ore.transform.SetParent(transform, true);

        globalOreList.Add(ore);

        return ore;
    }

    public Ore[] GetAllAvailableOfType(MineralType gatherType) {
        return globalOreList.Where(ore => ore.associatedTask == null).ToArray();
    }

    //public void AddOreToStorage(Ore ore, Refinery refinery) {

    //}

    //public Ore TakeOreFromStorage(Refinery refinery) {

    //}

    public bool ConsumeInBuilding(Unit oreHolder, Building building) {

        if(!unitOreDistribution.ContainsKey(oreHolder) || unitOreDistribution[oreHolder].Count == 0) {
            return false;
        }

        Ore anyOre = unitOreDistribution[oreHolder][0];

        unitOreDistribution[oreHolder].Remove(anyOre);

        GameObject.DestroyImmediate(anyOre.gameObject);

        return true;
    }

    public void GiveToUnit(Ore ore, Unit unit) {
        if(!unitOreDistribution.ContainsKey(unit)) {
            unitOreDistribution.Add(unit, new List<Ore>());
        }

        List<Ore> oreList = unitOreDistribution[unit];
        globalOreList.Remove(ore);
        oreList.Add(ore);
    }
}
