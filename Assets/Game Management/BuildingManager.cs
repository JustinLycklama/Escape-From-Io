using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    List<Building> buildingList = new List<Building>();
    BuildingEffectStatus[,] statusMap;

    public void Initialize() {
        MapsManager mapsManager = Script.Get<MapsManager>();
        Constants constants = Script.Get<Constants>();

        statusMap = new BuildingEffectStatus[constants.layoutMapWidth * mapsManager.horizontalMapCount, constants.layoutMapHeight * mapsManager.verticalMapCount];
    }


    public void BuildAt(Building building, LayoutCoordinate layoutCoordinate, BlueprintCost cost) {
        WorldPosition worldPosition = new WorldPosition(new MapCoordinate(layoutCoordinate));

        building.SetCost(cost);
        building.transform.position = worldPosition.vector3;

        Script.Get<GameResourceManager>().CueueGatherTasksForCost(cost, worldPosition, building);

        NotifyBuildingUpdate(building, true);
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
