using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public interface UnitManagerDelegate {
    void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType, Unit.UnitState unitListState);
}

public class UnitManager : MonoBehaviour, TaskStatusUpdateDelegate {

    Dictionary<MasterGameTask.ActionType, List<Unit>> unitListMap;
    Dictionary<MasterGameTask.ActionType, List<UnitManagerDelegate>> delegateListMap;

    Dictionary<MasterGameTask.ActionType, Unit.UnitState> unitListStateMap;

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

        unitListStateMap = new Dictionary<MasterGameTask.ActionType, Unit.UnitState>();

        foreach(MasterGameTask.ActionType actionType in new MasterGameTask.ActionType[] { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move, MasterGameTask.ActionType.Attack }) {
            unitListMap[actionType] = new List<Unit>();
            delegateListMap[actionType] = new List<UnitManagerDelegate>();

            unitListStateMap[actionType] = Unit.UnitState.Idle;
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

    public Unit[] GetAllPlayerUnits() {
        return GetPlayerUnitsOfType(MasterGameTask.ActionType.Build)
            .Concat(GetPlayerUnitsOfType(MasterGameTask.ActionType.Mine))
            .Concat(GetPlayerUnitsOfType(MasterGameTask.ActionType.Move))
            .Concat(GetPlayerUnitsOfType(MasterGameTask.ActionType.Attack))
            .ToArray() ?? new Unit[0];
    }

    public Unit[] GetPlayerUnitsOfType(MasterGameTask.ActionType type) {
        return unitListMap[type].Where(unit => unit.factionType == Unit.FactionType.Player).ToArray();
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

    private void RecalculateStatus(MasterGameTask.ActionType actionType) {

        if (unitListMap[actionType].Count == 0) {
            unitListStateMap[actionType] = Unit.UnitState.Idle;
        } else {

            Unit.UnitState lowestUnitState = Unit.UnitState.Efficient;

            foreach(Unit unit in unitListMap[actionType]) {
                Unit.UnitState unitState = unit.GetUnitState();

                if(unitState.ranking() < lowestUnitState.ranking()) {
                    lowestUnitState = unitState;
                }
            }

            unitListStateMap[actionType] = lowestUnitState;
        }

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

        notificationDelegate.NotifyUpdateUnitList(unitListMap[ofType].ToArray(), ofType, unitListStateMap[ofType]);
    }

    public void EndNotifications(UnitManagerDelegate notificationDelegate, MasterGameTask.ActionType forType) {
        delegateListMap[forType].Remove(notificationDelegate);
    }

    private void NotifyDelegates(MasterGameTask.ActionType forType) {
        foreach(UnitManagerDelegate notificationDelegate in delegateListMap[forType]) {
            notificationDelegate.NotifyUpdateUnitList(unitListMap[forType].ToArray(), forType, unitListStateMap[forType]);
        }
    }
}
