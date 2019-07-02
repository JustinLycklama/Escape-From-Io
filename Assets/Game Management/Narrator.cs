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
        //grid.gridWorldSize = new Vector2(map.mapWidth * mapContainer.gameObject.transform.localScale.x, map.mapHeight * mapContainer.gameObject.transform.localScale.z);

        grid.createGrid();
        grid.BlurPenaltyMap(4);

        //unit.GetComponent<Unit>().BeginQueueing();

        PathGridCoordinate[][] coordinatesForSpawnCoordinate = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(spawnCoordinate);

        int i = 0;
        foreach (Unit unit in startingUnits) {
            unit.Initialize();
            WorldPosition worldPos = new WorldPosition(MapCoordinate.FromGridCoordinate(coordinatesForSpawnCoordinate[1][i]));
            unit.transform.position = worldPos.vector3;
            i++;

            Script.Get<UnitManager>().RegisterUnit(unit);
        }

        Camera.main.transform.position = new WorldPosition(new MapCoordinate(spawnCoordinate)).vector3 + new Vector3(0, 250, -400);
    }
}
