﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface UnitManagerDelegate {
    void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType, Unit.UnitState unitListState);
}

public class UnitManager : MonoBehaviour, TaskStatusUpdateDelegate {

    Dictionary<MasterGameTask.ActionType, List<Unit>> unitListMap;
    Dictionary<MasterGameTask.ActionType, List<UnitManagerDelegate>> delegateListMap;

    Dictionary<MasterGameTask.ActionType, Unit.UnitState> unitListStateMap;

    void Awake() {
        unitListMap = new Dictionary<MasterGameTask.ActionType, List<Unit>>();
        delegateListMap = new Dictionary<MasterGameTask.ActionType, List<UnitManagerDelegate>>();

        unitListStateMap = new Dictionary<MasterGameTask.ActionType, Unit.UnitState>();

        foreach(MasterGameTask.ActionType actionType in new MasterGameTask.ActionType[] { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move }) {
            unitListMap[actionType] = new List<Unit>();
            delegateListMap[actionType] = new List<UnitManagerDelegate>();

            unitListStateMap[actionType] = Unit.UnitState.Idle;
        }
    }

    public Unit[] GetUnitsOfType(MasterGameTask.ActionType type) {
        return unitListMap[type].ToArray();
    }

    public void RegisterUnit(Unit unit) {
        unitListMap[unit.primaryActionType].Add(unit);
        NotifyDelegates(unit.primaryActionType);

        // Sign up for unit's task updates so we can calculate status
        unit.RegisterForTaskStatusNotifications(this);
    }

    public void DisableUnit(Unit unit) {
        unit.EndTaskStatusNotifications(this);
    }

    private void RecalculateStatus(MasterGameTask.ActionType actionType) {

        if (unitListMap[actionType].Count == 0) {
            unitListStateMap[actionType] = Unit.UnitState.Idle;
        } else {

            Unit.UnitState lowestUnitState = Unit.UnitState.Efficient;

            foreach(Unit unit in unitListMap[actionType]) {
                Unit.UnitState unitState = Unit.UnitState.Idle;

                if(unit.currentMasterTask != null) {
                    if(unit.currentMasterTask.actionType == unit.primaryActionType) {
                        unitState = Unit.UnitState.Efficient;
                    } else {
                        unitState = Unit.UnitState.Inefficient;
                    }
                }

                if(unitState.ranking() < lowestUnitState.ranking()) {
                    lowestUnitState = unitState;
                }
            }

            unitListStateMap[actionType] = lowestUnitState;
        }

        NotifyDelegates(actionType);
    }

    public void BuildAt(Unit unit, LayoutCoordinate layoutCoordinate, BlueprintCost cost) {
        WorldPosition worldPosition = new WorldPosition(new MapCoordinate(layoutCoordinate));

        unit.SetCost(cost);
        unit.transform.position = worldPosition.vector3;

        Script.Get<GameResourceManager>().CueueGatherTasksForCost(cost, worldPosition, unit);

        NotifyDelegates(unit.primaryActionType);
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
