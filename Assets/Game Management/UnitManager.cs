using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public interface UnitManagerDelegate {
    void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType);
}

public class UnitManager : MonoBehaviour, TaskStatusUpdateDelegate {

    [Serializable]
    private struct ActionIconObj {
        public MasterGameTask.ActionType actionType;
        public Sprite sprite;
    }


    [SerializeField]
    private List<ActionIconObj> actionIconObjList;

    Dictionary<MasterGameTask.ActionType, List<Unit>> unitListMap;
    Dictionary<MasterGameTask.ActionType, List<UnitManagerDelegate>> delegateListMap;

    //Dictionary<MasterGameTask.ActionType, Unit.UnitState> unitListStateMap;

    public static Dictionary<MasterGameTask.ActionType, int> backingUnitCount;
    public static Dictionary<MasterGameTask.ActionType, int> unitCount {
        get {
            if (backingUnitCount == null) {
                backingUnitCount = new Dictionary<MasterGameTask.ActionType, int>();

                unitCount[MasterGameTask.ActionType.Build] = 0;
                unitCount[MasterGameTask.ActionType.Mine] = 0;
                unitCount[MasterGameTask.ActionType.Move] = 0;
                unitCount[MasterGameTask.ActionType.Attack] = 0;
            }

            return backingUnitCount;
        }
    }

    void Awake() {

        unitListMap = new Dictionary<MasterGameTask.ActionType, List<Unit>>();
        delegateListMap = new Dictionary<MasterGameTask.ActionType, List<UnitManagerDelegate>>();

        //unitListStateMap = new Dictionary<MasterGameTask.ActionType, Unit.UnitState>();

        foreach(MasterGameTask.ActionType actionType in new MasterGameTask.ActionType[] { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move, MasterGameTask.ActionType.Attack }) {
            unitListMap[actionType] = new List<Unit>();
            delegateListMap[actionType] = new List<UnitManagerDelegate>();

            //unitListStateMap[actionType] = Unit.UnitState.Idle;
        }
    }

    private void Start() {
        SceneManagement.sharedInstance.sceneUnloadEvent += () => {
            unitCount[MasterGameTask.ActionType.Build] = 0;
            unitCount[MasterGameTask.ActionType.Mine] = 0;
            unitCount[MasterGameTask.ActionType.Move] = 0;
            unitCount[MasterGameTask.ActionType.Attack] = 0;
        };
    }

    public Sprite UnitIconForActionType(MasterGameTask.ActionType actionType) {
        return actionIconObjList.Where(obj => obj.actionType == actionType).First().sprite;
    }

    public Unit[] GetAllPlayerUnits(Unit.FactionType faction = Unit.FactionType.Player) {
        return GetPlayerUnitsOfType(MasterGameTask.ActionType.Build, faction)
            .Concat(GetPlayerUnitsOfType(MasterGameTask.ActionType.Mine, faction))
            .Concat(GetPlayerUnitsOfType(MasterGameTask.ActionType.Move, faction))
            .Concat(GetPlayerUnitsOfType(MasterGameTask.ActionType.Attack, faction))
            .ToArray() ?? new Unit[0];
    }

    public Unit[] GetPlayerUnitsOfType(MasterGameTask.ActionType type, Unit.FactionType faction = Unit.FactionType.Player) {
        return unitListMap[type].Where(unit => unit.factionType == faction).ToArray();
    }

    public void RegisterUnit(Unit unit) {
        unitListMap[unit.primaryActionType].Add(unit);
        NotifyDelegates(unit.primaryActionType);

        // Sign up for unit's task updates so we can calculate status
        unit.RegisterForTaskStatusNotifications(this);
    }

    public void DisableUnit(Unit unit) {
        unitListMap[unit.primaryActionType].Remove(unit);
        NotifyDelegates(unit.primaryActionType);

        unit.EndTaskStatusNotifications(this);
    }

    public bool IsUnitEnabled(Unit unit) {
        if (unit == null) {
            return false;
        }

        return unitListMap[unit.primaryActionType].Contains(unit);
    }

    private void RecalculateStatus(MasterGameTask.ActionType actionType) {

        //if (unitListMap[actionType].Count == 0) {
        //    unitListStateMap[actionType] = Unit.UnitState.Idle;
        //} else {

        //    Unit.UnitState lowestUnitState = Unit.UnitState.Efficient;

        //    foreach(Unit unit in unitListMap[actionType]) {
        //        Unit.UnitState unitState = unit.GetUnitState();

        //        if(unitState.ranking() < lowestUnitState.ranking()) {
        //            lowestUnitState = unitState;
        //        }
        //    }

        //    unitListStateMap[actionType] = lowestUnitState;
        //}

        NotifyDelegates(actionType);
    }

    public void BuildAt(UnitBuilding unitBuilding, LayoutCoordinate layoutCoordinate, BlueprintCost cost) {
        WorldPosition worldPosition = new WorldPosition(new MapCoordinate(layoutCoordinate));

        unitBuilding.SetCost(cost);
        unitBuilding.transform.position = worldPosition.vector3;

        Script.Get<GameResourceManager>().CueueGatherTasksForCost(cost, worldPosition, unitBuilding);

        NotifyDelegates(unitBuilding.associatedUnit.primaryActionType);
    }

    /*
    * TaskStatusUpdateDelegate Interface
    * */

    public void NowPerformingTask(Unit unit, MasterGameTask masterGameTask, GameTask gameTask) {
        RecalculateStatus(unit.primaryActionType);
    }

    /*
     * Methods for UnitManagerDelegate
     * */

    public void RegisterForNotifications(UnitManagerDelegate notificationDelegate, MasterGameTask.ActionType ofType) {
        delegateListMap[ofType].Add(notificationDelegate);

        notificationDelegate.NotifyUpdateUnitList(unitListMap[ofType].ToArray(), ofType); //  unitListStateMap[forType]
    }

    public void EndNotifications(UnitManagerDelegate notificationDelegate, MasterGameTask.ActionType forType) {
        delegateListMap[forType].Remove(notificationDelegate);
    }

    private void NotifyDelegates(MasterGameTask.ActionType forType) {
        foreach(UnitManagerDelegate notificationDelegate in delegateListMap[forType]) {
            notificationDelegate.NotifyUpdateUnitList(unitListMap[forType].ToArray(), forType); // unitListStateMap[forType]
        }
    }
}
