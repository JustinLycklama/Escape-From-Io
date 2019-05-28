using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Narrator : MonoBehaviour
{
    PathfindingGrid grid;
    MapGenerator mapGenerator;
    MapContainer mapContainer;

    Constants constants;

    public GameObject unit;

    Material mapMaterial;

    // Start is called before the first frame update
    void Start() {
        grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
        mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
        mapContainer = Tag.Map.GetGameObject().GetComponent<MapContainer>();
        constants = GetComponent<Constants>();

        mapMaterial = Tag.Map.GetGameObject().GetComponent<MeshRenderer>().material;

        // Pass in "towers"
        //mapMaterial.SetVectorArray


        // Setup world
        Map map = mapGenerator.GenerateMap();
        mapContainer.setMap(map);

        grid.gameObject.transform.position = mapContainer.transform.position;
        //grid.gridWorldSize = new Vector2(map.mapWidth * mapContainer.gameObject.transform.localScale.x, map.mapHeight * mapContainer.gameObject.transform.localScale.z);

        grid.createGrid(map);
        grid.BlurPenaltyMap(4);

        unit.GetComponent<Unit>().BeginQueueing();
    }

    // Update is called once per frame
    void Update() {
        
    }
}
