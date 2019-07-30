using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface BuildingsUpdateDelegate {
    void NewBuildingStarted(Building building);
    void BuildingFinished(Building building);
    //void OreRemoved(Ore ore);
}

public class BuildingManager : MonoBehaviour {

    enum LayoutCoordinateStatus {
        Light = (1 << 0), //1 in decimal
        Scan = (1 << 1), //2 in decimal
        //Up = (1 << 2), //4 in decimal
    }

    List<Building> buildingList = new List<Building>();
    LayoutCoordinateStatus[,] statusMap;

    public void Initialize() {
        MapsManager mapsManager = Script.Get<MapsManager>();
        Constants constants = Script.Get<Constants>();

        statusMap = new LayoutCoordinateStatus[constants.layoutMapWidth * mapsManager.horizontalMapCount, constants.layoutMapHeight * mapsManager.verticalMapCount];
    }


    public void BuildAt(Building building, LayoutCoordinate layoutCoordinate, BlueprintCost cost) {
        WorldPosition worldPosition = new WorldPosition(new MapCoordinate(layoutCoordinate));

        building.SetCost(cost);
        building.transform.position = worldPosition.vector3;

        Script.Get<GameResourceManager>().CueueGatherTasksForCost(cost, worldPosition, building);

        NotifyBuildingUpdate(building, true);
    }

    public void CompleteBuilding(Building building) {
        NotifyBuildingUpdate(building, false);
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
}
