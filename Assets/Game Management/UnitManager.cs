using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface UnitManagerDelegate {
    void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType);
}

public class UnitManager : MonoBehaviour {

    Dictionary<MasterGameTask.ActionType, List<Unit>> unitListMap;
    Dictionary<MasterGameTask.ActionType, List<UnitManagerDelegate>> delegateListMap;

    void Awake() {
        unitListMap = new Dictionary<MasterGameTask.ActionType, List<Unit>>();
        delegateListMap = new Dictionary<MasterGameTask.ActionType, List<UnitManagerDelegate>>();

        foreach(MasterGameTask.ActionType actionType in new MasterGameTask.ActionType[] { MasterGameTask.ActionType.Build, MasterGameTask.ActionType.Mine, MasterGameTask.ActionType.Move }) {
            unitListMap[actionType] = new List<Unit>();
            delegateListMap[actionType] = new List<UnitManagerDelegate>();
        }
    }

    public Unit[] GetUnitsOfType(MasterGameTask.ActionType type) {
        return unitListMap[type].ToArray();
    }

    public void RegisterForNotifications(UnitManagerDelegate notificationDelegate, MasterGameTask.ActionType ofType) {
        delegateListMap[ofType].Add(notificationDelegate);

        notificationDelegate.NotifyUpdateUnitList(unitListMap[ofType].ToArray(), ofType);
    }

    public void EndNotifications(UnitManagerDelegate notificationDelegate, MasterGameTask.ActionType forType) {
        delegateListMap[forType].Remove(notificationDelegate);
    }

    private void NotifyDelegates(MasterGameTask.ActionType forType) {
        foreach(UnitManagerDelegate notificationDelegate in delegateListMap[forType]) {
            notificationDelegate.NotifyUpdateUnitList(unitListMap[forType].ToArray(), forType);
        }
    }

    //public T CreateUnit<T: Unit>() {


    //}

    public void RegisterUnit(Unit unit) {
        unitListMap[unit.primaryActionType].Add(unit);
        NotifyDelegates(unit.primaryActionType);
    }
}
