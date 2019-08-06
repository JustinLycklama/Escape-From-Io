using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Narrator : MonoBehaviour
{
    PathfindingGrid grid;
    MapGenerator mapGenerator;
    MapsManager mapsManager;

    Constants constants;

    public List<Unit> startingUnits;

    LayoutCoordinate spawnCoordinate;

    void Start() {
        grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
        mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
        mapsManager = Script.Get<MapsManager>();
        constants = GetComponent<Constants>();

        spawnCoordinate = mapGenerator.GenerateWorld(constants.mapCountX, constants.mapCountY);

        grid.gameObject.transform.position = mapsManager.transform.position;
        grid.createGrid();
        //grid.BlurPenaltyMap(4); // No blurr today!        

        Script.Get<BuildingManager>().Initialize();


        PathGridCoordinate[][] coordinatesForSpawnCoordinate = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(spawnCoordinate);

        int i = 0;
        foreach (Unit unit in startingUnits) {

            int horizontalComponent = 1;
            if (i == 1) {
                horizontalComponent = 0;
            }

            WorldPosition worldPos = new WorldPosition(MapCoordinate.FromGridCoordinate(coordinatesForSpawnCoordinate[horizontalComponent][i]));
            unit.transform.position = worldPos.vector3;
            i++;

            UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();

            if (unitBuilding != null) {
                unitBuilding.ProceedToCompleteBuilding();
            } else {
                unit.Initialize();
            }            
        }

        WorldPosition spawnWorldPosition = new WorldPosition(new MapCoordinate(spawnCoordinate));

        Building building = Instantiate(Building.Blueprint.Tower.resource) as Building;
        building.transform.position = spawnWorldPosition.vector3;

        //buildingManager.BuildAt(building, spawnCoordinate, new BlueprintCost(1, 1, 1));
        building.ProceedToCompleteBuilding();

        Camera.main.transform.position = spawnWorldPosition.vector3 + new Vector3(0, 250, -400);

        Script.Get<MiniMap>().Initialize();
    }
}
