using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Narrator : MonoBehaviour
{
    Grid grid;
    MapGenerator mapGenerator;
    MapContainer mapContainer;

    Constants constants;

    public GameObject unit;

    // Start is called before the first frame update
    void Start() {
        grid = Tag.AStar.GetGameObject().GetComponent<Grid>();
        mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
        mapContainer = Tag.Map.GetGameObject().GetComponent<MapContainer>();
        constants = GetComponent<Constants>();

        // Setup world
        Map map = mapGenerator.GenerateMap();
        mapContainer.setMap(map);

        grid.gameObject.transform.position = mapContainer.transform.position;
        grid.gridWorldSize = new Vector2(map.mapWidth * mapContainer.gameObject.transform.localScale.x, map.mapHeight * mapContainer.gameObject.transform.localScale.z);

        grid.createGrid();
        grid.BlurPenaltyMap(4);

        unit.GetComponent<Unit>().beginPathFinding();
    }

    // Update is called once per frame
    void Update() {
        
    }
}
