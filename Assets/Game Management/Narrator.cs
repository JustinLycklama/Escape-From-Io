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

    // Start is called before the first frame update
    void Start() {
        grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
        mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
        mapsManager = Script.Get<MapsManager>();
        constants = GetComponent<Constants>();

        mapsManager.InitializeMaps(constants.mapCountX, constants.mapCountY);

        // Setup world
        foreach (MapContainer container in mapsManager.mapContainers) {
            Map map = mapGenerator.GenerateMap(container);
            container.setMap(map);
        }
     
        grid.gameObject.transform.position = mapsManager.transform.position;
        //grid.gridWorldSize = new Vector2(map.mapWidth * mapContainer.gameObject.transform.localScale.x, map.mapHeight * mapContainer.gameObject.transform.localScale.z);

        grid.createGrid();
        grid.BlurPenaltyMap(4);

        //unit.GetComponent<Unit>().BeginQueueing();

        foreach (Unit unit in startingUnits) {
            unit.Init();
        }
    }

    // Update is called once per frame
    void Update() {
        
    }
}
