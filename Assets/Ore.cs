using System;
using System.Collections.Generic;
using UnityEngine;

public class GameResourceManager {
    public static GameResourceManager sharedInstance = new GameResourceManager();

    private class Blueprint : PrefabBlueprint {
        public static Blueprint Basic = new Blueprint("Ore", "StandardOre", typeof(Ore));

        private Blueprint(string fileName, string description, Type type) : base(fileName, description, type) { }
    }

    List<Ore> globalOreList;
    Dictionary<Refinery, Ore[]> refineryOreDistribution;

    public enum GatherType { Ore }

    private GameResourceManager() {

        globalOreList = new List<Ore>();
        refineryOreDistribution = new Dictionary<Refinery, Ore[]>();
    }

    public Ore CreateOre() {
        Ore ore = Blueprint.Basic.Instantiate() as Ore;

        globalOreList.Add(ore);

        return ore;
    }

    public Ore[] GetAllOfType(GatherType gatherType) {
        return globalOreList.ToArray();
    }

    //public void AddOreToStorage(Ore ore, Refinery refinery) {

    //}

    //public Ore TakeOreFromStorage(Refinery refinery) {

    //}

    public void ConsumeOreInBuilding(Ore ore, Building building) {
        globalOreList.Remove(ore);

    }
}

public class Ore : MonoBehaviour, ActionableItem {
    public string description => throw new NotImplementedException();

    public float performAction(GameTask task, float rate) {
        return 1;
    }
}
