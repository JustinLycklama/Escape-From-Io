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

        TaskQueueManager queue = Script.Get<TaskQueueManager>();
        List<MasterGameTask> blockingBuildTasks = new List<MasterGameTask>();

        foreach(MineralType mineralType in cost.costMap.Keys) {

            GameTask oreTask = new GameTask("Find Ore", mineralType, GameTask.ActionType.PickUp, null);
            oreTask.SatisfiesStartRequirements = () => {
                return Script.Get<GameResourceManager>().AnyMineralAvailable(mineralType);
            };

            GameTask dropTask = new GameTask("Deposit Ore", worldPosition, GameTask.ActionType.DropOff, building, PathRequestTargetType.PathGrid);

            MasterGameTask masterCollectTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Collect Ore " + mineralType.ToString(), new GameTask[] { oreTask, dropTask });
            masterCollectTask.repeatCount = cost.costMap[mineralType];

            queue.QueueTask(masterCollectTask);
            blockingBuildTasks.Add(masterCollectTask);
        }

        GameTask buildTask = new GameTask("Construction", worldPosition, GameTask.ActionType.Build, building, PathRequestTargetType.PathGrid);
        MasterGameTask masterBuildTask = new MasterGameTask(MasterGameTask.ActionType.Build, "Build Building " + building.description, new GameTask[] { buildTask }, blockingBuildTasks);

        queue.QueueTask(masterBuildTask);

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
