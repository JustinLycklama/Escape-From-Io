using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameResourceManager {
    public static GameResourceManager sharedInstance = new GameResourceManager();

    private class Blueprint : PrefabBlueprint {
        public static Blueprint Basic = new Blueprint("Ore", "StandardOre", typeof(Ore));

        private Blueprint(string fileName, string description, Type type) : base(fileName, description, type) { }
    }

    List<Ore> globalOreList;

    Dictionary<Refinery, Ore[]> refineryOreDistribution;
    Dictionary<Unit, List<Ore>> unitOreDistribution;

    public enum GatherType { Ore }

    private GameResourceManager() {

        globalOreList = new List<Ore>();

        refineryOreDistribution = new Dictionary<Refinery, Ore[]>();
        unitOreDistribution = new Dictionary<Unit, List<Ore>>();
    }

    public bool AnyOreAvailable() {
        return GetAllAvailableOfType(GatherType.Ore).Count() > 0;
    }

    public Ore CreateOre() {
        Ore ore = Blueprint.Basic.Instantiate() as Ore;

        globalOreList.Add(ore);

        return ore;
    }

    public Ore[] GetAllAvailableOfType(GatherType gatherType) {
        return globalOreList.Where(ore => ore.associatedTask == null).ToArray();
    }

    //public void AddOreToStorage(Ore ore, Refinery refinery) {

    //}

    //public Ore TakeOreFromStorage(Refinery refinery) {

    //}

    public bool ConsumeInBuilding(Unit oreHolder, Building building) {
        
        if (!unitOreDistribution.ContainsKey(oreHolder) || unitOreDistribution[oreHolder].Count == 0) {
            return false;
        }

        Ore anyOre = unitOreDistribution[oreHolder][0];

        unitOreDistribution[oreHolder].Remove(anyOre);

        GameObject.DestroyImmediate(anyOre.gameObject);

        return true;
    }

    public void GiveToUnit(Ore ore, Unit unit) {
        if (!unitOreDistribution.ContainsKey(unit)) {
            unitOreDistribution.Add(unit, new List<Ore>());
        }

        List<Ore> oreList = unitOreDistribution[unit];
        globalOreList.Remove(ore);
        oreList.Add(ore);
    }
}

public class Ore : MonoBehaviour, ActionableItem {
    public string description => throw new NotImplementedException();


    public Unit currentCarrier;

    float pickingUpPercent = 0;

    public GameTask associatedTask;
    public void AssociateTask(GameTask task) {
        associatedTask = task;
    }

    public float performAction(GameTask task, float rate, Unit unit) {
        switch(task.action) {

            case GameTask.ActionType.PickUp:
                pickingUpPercent += rate;

                if (pickingUpPercent >= 1) {
                    pickingUpPercent = 1;

                    // The associatedTask is over
                    associatedTask = null;

                    GameResourceManager.sharedInstance.GiveToUnit(this, unit);
                    this.transform.SetParent(unit.transform, true);
                }

                break;
            case GameTask.ActionType.DropOff:
                break;

            case GameTask.ActionType.Build:
            case GameTask.ActionType.Mine:
            default:
                throw new NotImplementedException();
        }

        return pickingUpPercent;
    }
}
