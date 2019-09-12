using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public interface BuildingsUpdateDelegate {
    void NewBuildingStarted(Building building);
    void BuildingFinished(Building building);
}

public interface StatusEffectUpdateDelegate {
    void StatusEffectMapUpdated(BuildingEffectStatus[,] statusMap);
}

public enum BuildingEffectStatus {
    None = (0 << 0),
    Light = (1 << 0), //1 in decimal
    Scan = (1 << 1), //2 in decimal
                     //Up = (1 << 2), //4 in decimal
}

public class BuildingManager : MonoBehaviour {

    BuildingEffectStatus[,] statusMap;
    Dictionary<LayoutCoordinate, Building> locationBuildingMap = new Dictionary<LayoutCoordinate, Building>();

    private List<LayoutCoordinate> listOfCoordinatesToBlockTerrain = new List<LayoutCoordinate>();

    public void Initialize() {
        MapsManager mapsManager = Script.Get<MapsManager>();
        Constants constants = Script.Get<Constants>();

        statusMap = new BuildingEffectStatus[constants.layoutMapWidth * mapsManager.horizontalMapCount, constants.layoutMapHeight * mapsManager.verticalMapCount];

        StartCoroutine(AttemptToSetUnwalkable());
    }

    public Building buildlingAtLocation(LayoutCoordinate layoutCoordinate) {
        if(locationBuildingMap.ContainsKey(layoutCoordinate)) {
            return locationBuildingMap[layoutCoordinate];
        }

        return null;
    }

    public void BuildAt(Building building, LayoutCoordinate layoutCoordinate, BlueprintCost cost, bool asLastPriority) {
        WorldPosition worldPosition = new WorldPosition(new MapCoordinate(layoutCoordinate));

        building.SetCost(cost);
        building.transform.position = worldPosition.vector3;

        Script.Get<GameResourceManager>().CueueGatherTasksForCost(cost, worldPosition, building, asLastPriority);

        AddBuildingAtLocation(building, layoutCoordinate);

        NotifyBuildingUpdate(building, true);
    }

    public void AddBuildingAtLocation(Building building, LayoutCoordinate layoutCoordinate) {
        // Don't record "PathBuilding" buildings for blocking purposes
        if(building.title != "Path") {
            locationBuildingMap[layoutCoordinate] = building;
            if(AttemptToSetUnwalkable(layoutCoordinate) == false) {
                listOfCoordinatesToBlockTerrain.Add(layoutCoordinate);
            }
        }
    }

    public void CompleteBuilding(Building building) {
        Constants constants = Script.Get<Constants>();

        WorldPosition worldPosition = new WorldPosition(building.transform.position);
        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);
        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

        int x = layoutCoordinate.mapContainer.mapX * constants.layoutMapWidth + layoutCoordinate.x;
        int y = layoutCoordinate.mapContainer.mapY * constants.layoutMapHeight + layoutCoordinate.y;

        ModifyStatus(x, y, building.BuildingStatusEffects(), building.BuildingStatusRange());

        NotifyBuildingUpdate(building, false);
    }

    public void RemoveBuilding(Building buildling) {
        WorldPosition worldPosition = new WorldPosition(buildling.transform.position);
        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);
        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

        locationBuildingMap.Remove(layoutCoordinate);
        layoutCoordinate.mapContainer.map.UpdateUserActionsAt(layoutCoordinate);

        listOfCoordinatesToBlockTerrain.Remove(layoutCoordinate);
        Script.Get<PathfindingGrid>().SetCenterGridWalkable(layoutCoordinate, true);
    }

    private IEnumerator AttemptToSetUnwalkable() {

        while(true) {

            foreach(LayoutCoordinate layoutCoordinate in listOfCoordinatesToBlockTerrain.ToArray()) {
                bool success = AttemptToSetUnwalkable(layoutCoordinate);

                if (success) {
                    listOfCoordinatesToBlockTerrain.Remove(layoutCoordinate);
                }
            }

            yield return new WaitForSeconds(1);
        }
    }

    private bool AttemptToSetUnwalkable(LayoutCoordinate layoutCoordinate) {

        UnitManager unitManager = Script.Get<UnitManager>();

        Unit[] allUnits = unitManager.GetUnitsOfType(MasterGameTask.ActionType.Build).Concat(unitManager.GetUnitsOfType(MasterGameTask.ActionType.Mine)).Concat(unitManager.GetUnitsOfType(MasterGameTask.ActionType.Move)).ToArray();

        MapCoordinate centerOfLayout = new MapCoordinate(layoutCoordinate);

        foreach(Unit unit in allUnits) {
            WorldPosition worldPosition = new WorldPosition(unit.transform.position);
            MapCoordinate unitMapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

            if (unitMapCoordinate == centerOfLayout) {
                return false;
            }
        }

        // Update pathfinding grid
        Script.Get<PathfindingGrid>().SetCenterGridWalkable(layoutCoordinate, false);
        return true;
    }

    private void ModifyStatus(int centerX, int centerY, BuildingEffectStatus status, int radius) {

        if (radius == 0) {
            return;
        }

        Constants constants = Script.Get<Constants>();
        MapsManager mapsManager = Script.Get<MapsManager>();

        int maxX = constants.layoutMapWidth * mapsManager.horizontalMapCount;
        int maxY = constants.layoutMapHeight * mapsManager.verticalMapCount;

        for(int x = centerX - radius; x <= centerX + radius; x++) {
            for(int y = centerY - radius; y <= centerY + radius; y++) {

                // Do not illuminate corners
                if ((x == centerX - radius || x == centerX + radius) && (y == centerY - radius || y == centerY + radius)) {
                    continue;
                }

                int clampedX = Mathf.Clamp(x, 0, maxX);
                int clampedY = Mathf.Clamp(y, 0, maxY);

                statusMap[clampedX, clampedY] |= status;
            }
        }

        NotifyStatusEffectUpdate();
    }

    /*
     * Methods for BuildingsUpdateDelegate
     * */

    public List<BuildingsUpdateDelegate> buildingUpdateDelegateList = new List<BuildingsUpdateDelegate>();

    public void RegisterFoBuildingNotifications(BuildingsUpdateDelegate notificationDelegate) {
        buildingUpdateDelegateList.Add(notificationDelegate);
    }

    public void EndBuildingNotifications(BuildingsUpdateDelegate notificationDelegate) {
        buildingUpdateDelegateList.Remove(notificationDelegate);
    }

    public void NotifyBuildingUpdate(Building building, bool isNew) {
        foreach(BuildingsUpdateDelegate updateDelegate in buildingUpdateDelegateList) {
            if(isNew) {
                updateDelegate.NewBuildingStarted(building);
            } else {
                updateDelegate.BuildingFinished(building);
            }
        }
    }

    /*
     * Methods For StatusEffectUpdateDelegate
     * */

    public List<StatusEffectUpdateDelegate> statusEffectUpdateDelegateList = new List<StatusEffectUpdateDelegate>();

    public void RegisterForStatusEffectNotifications(StatusEffectUpdateDelegate notificationDelegate) {
        statusEffectUpdateDelegateList.Add(notificationDelegate);

        // Do not notify, everyone is registering for notifications on app start
        //notificationDelegate.StatusEffectMapUpdated(statusMap);
    }

    public void EndStatusEffectNotifications(StatusEffectUpdateDelegate notificationDelegate) {
        statusEffectUpdateDelegateList.Remove(notificationDelegate);
    }

    public void NotifyStatusEffectUpdate() {
        foreach(StatusEffectUpdateDelegate updateDelegate in statusEffectUpdateDelegateList) {
                updateDelegate.StatusEffectMapUpdated(statusMap);
        }
    }

}
