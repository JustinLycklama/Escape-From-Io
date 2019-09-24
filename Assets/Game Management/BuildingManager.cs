using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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
    Building[,] buildingsEffectingStatusMapMap;

    Dictionary<LayoutCoordinate, Building> locationBuildingMap = new Dictionary<LayoutCoordinate, Building>();
    private List<LayoutCoordinate> listOfCoordinatesToBlockTerrain = new List<LayoutCoordinate>();

    public void Initialize() {
        MapsManager mapsManager = Script.Get<MapsManager>();
        Constants constants = Script.Get<Constants>();

        int width = constants.layoutMapWidth * mapsManager.horizontalMapCount;
        int height = constants.layoutMapHeight * mapsManager.verticalMapCount;

        statusMap = new BuildingEffectStatus[width, height];
        buildingsEffectingStatusMapMap = new Building[width, height];

        StartCoroutine(AttemptToSetUnwalkable());
    }

    public Building buildlingAtLocation(LayoutCoordinate layoutCoordinate) {
        if(locationBuildingMap.ContainsKey(layoutCoordinate)) {
            return locationBuildingMap[layoutCoordinate];
        }

        return null;
    }

    public bool IsLayoutCoordinateAdjacentToBuilding(LayoutCoordinate layoutCoordinate, Type buildingType, bool requiresComplete = true) {
        foreach(LayoutCoordinate adjCoordinate in layoutCoordinate.AdjacentCoordinates()) {
            if (locationBuildingMap.ContainsKey(adjCoordinate)) {

                Building building = locationBuildingMap[adjCoordinate];
                if((building.buildingComplete || !requiresComplete) && building.GetType() == buildingType) {
                    return true;
                }                
            }
        }

        return false;
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
        building.transform.SetParent(transform, true);

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

        if (building.BuildingStatusEffects() != BuildingEffectStatus.None) {
            AddBuildingAffectingStatusMap(building, layoutCoordinate);
        }
        
        //ModifyStatusCircularAroundPoint(x, y, building.BuildingStatusEffects(), building.BuildingStatusRange());

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

        Unit[] allUnits = unitManager.GetAllUnits();

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

    private void AddBuildingAffectingStatusMap(Building building, LayoutCoordinate layoutCoordinate) {
        Constants constants = Script.Get<Constants>();

        int startX = layoutCoordinate.mapContainer.mapX * constants.layoutMapWidth;
        int startY = layoutCoordinate.mapContainer.mapY * constants.layoutMapHeight;

        buildingsEffectingStatusMapMap[startX + layoutCoordinate.x, startY + layoutCoordinate.y] = building;

        RecalcluateSightStatuses();
    }

    public void RecalcluateSightStatuses() {
        Constants constants = Script.Get<Constants>();

        MapsManager mapsManager = Script.Get<MapsManager>();
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        int width = constants.layoutMapWidth * mapsManager.horizontalMapCount;
        int height = constants.layoutMapHeight * mapsManager.verticalMapCount;


        // Another option for visibility casting

        /*
        int buildingX = 0, buildingY = 0;

        
        AdamMilazzoVision visibilityCasting = new AdamMilazzoVision(delegate (int x, int y) {
            if (x < 0 || y < 0 || x >= width || y >= height) {
                return true;
            }

            return mapGenerator.GetTerrainAtAbsoluteXY(x, y).regionType == RegionType.Type.Mountain;
        },
        (int x, int y) => {
            if(x < 0 || y < 0 || x >= width || y >= height) {
                return;
            }

            statusMap[x, y] |= BuildingEffectStatus.Light;
        },
        delegate (int x, int y) {
            return Mathf.RoundToInt(Vector2.Distance(new Vector2(buildingX, buildingY), new Vector2(buildingX + x, buildingY + y)));
        });
        */





        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                Building building = buildingsEffectingStatusMapMap[x, y];

                if(building == null) {
                    continue;
                }

                if(building.BuildingStatusEffects() == BuildingEffectStatus.Light) {

                    // Another option for visibility casting

                    /*                                         
                    buildingX = x; buildingY = y;
                    visibilityCasting.Compute((uint)x, (uint)y, building.BuildingStatusRange());
                    */

                    EricLippertShadowCast.ComputeFieldOfViewWithShadowCasting(x, y, building.BuildingStatusRange(),
                        delegate (int subX, int subY) {
                            if(subX < 0 || subY < 0 || subX >= width || subY >= height) {
                                return true;
                            }

                            return mapGenerator.GetTerrainAtAbsoluteXY(subX, subY).regionType == RegionType.Type.Mountain;
                        },
                        (int subX, int subY) => {
                            if(subX < 0 || subY < 0 || subX >= width || subY >= height) {
                                return;
                            }

                            statusMap[subX, subY] |= BuildingEffectStatus.Light;
                        }
                        );
                }           
            }
        }

        VisionArtifactRemover artifactRemover = new VisionArtifactRemover(statusMap, buildingsEffectingStatusMapMap);
        artifactRemover.RemoveIslands();
        artifactRemover.RemoveLayerOfIsolatedTiles();

        NotifyStatusEffectUpdate();
    }

    // x and y should be anyPoint, x2 and y2 should be LightPoint
    public bool BresenhamIncludesMountain(int x, int y, int x2, int y2) {

        Constants constants = Script.Get<Constants>();
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        int originalX = x;
        int originalY = y;

        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if(w < 0) dx1 = -1; else if(w > 0) dx1 = 1;
        if(h < 0) dy1 = -1; else if(h > 0) dy1 = 1;
        if(w < 0) dx2 = -1; else if(w > 0) dx2 = 1;
        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);
        if(!(longest > shortest)) {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if(h < 0) dy2 = -1; else if(h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for(int i = 0; i <= longest; i++) {

            if (originalX != x || originalY != y) {
                if(mapGenerator.GetTerrainAtAbsoluteXY(x, y).regionType == RegionType.Type.Mountain) {
                    return true;
                }
            }
           
            numerator += shortest;
            if(!(numerator < longest)) {
                numerator -= longest;
                x += dx1;
                y += dy1;
            } else {
                x += dx2;
                y += dy2;
            }
        }

        return false;
    }

    private void ModifyStatusCircularAroundPoint(int centerX, int centerY, BuildingEffectStatus status, int radius) {

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
