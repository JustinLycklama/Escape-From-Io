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

    public static T[,] RangeSubset<T>(T[,] array, int startIndexX, int startIndexY, int lengthX, int lengthY) {
        T[,] subset = new T[lengthX, lengthY];

        for(int x = startIndexX; x < startIndexX + lengthX; x++) {
            for(int y = startIndexY; y < startIndexY + lengthY; y++) {
                subset[x - startIndexX, y - startIndexY] = array[x, y];
            }
        }

        return subset;
    }


    // Start is called before the first frame update
    void Start() {
        grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
        mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
        mapsManager = Script.Get<MapsManager>();
        constants = GetComponent<Constants>();

        mapsManager.InitializeMaps(constants.mapCountX, constants.mapCountY);

        int mapWidth = constants.layoutMapWidth;
        int mapHeight = constants.layoutMapHeight;

        int totalWidth = mapWidth * constants.mapCountX;
        int totalHeight = mapHeight * constants.mapCountY;

        float[,] layoutNoiseMap = mapGenerator.GenerateLayoutMap(totalWidth, totalHeight);

        float[,] groundFeaturesNoiseMap = mapGenerator.GenerateGroundFeaturesMap(totalWidth * constants.featuresPerLayoutPerAxis, totalHeight * constants.featuresPerLayoutPerAxis);
        float[,] mountainFeaturesNoiseMap = mapGenerator.GenerateMountainFeaturesMap(totalWidth * constants.featuresPerLayoutPerAxis, totalHeight * constants.featuresPerLayoutPerAxis);


        // Setup world
        foreach(MapContainer container in mapsManager.mapContainers) {
            int startX = container.mapX * mapWidth;
            int startY = container.mapY * mapHeight;

            //int xOffset = (container.mapX == 0 ? 0 : 1);
            //int yOffset = (container.mapY == 0 ? 0 : 1);

            float[,] mapLayoutNoise = RangeSubset(layoutNoiseMap, startX, startY, mapWidth, mapHeight);

            float[,] groundFeaturesLayoutNoise = RangeSubset(groundFeaturesNoiseMap, 
                startX * constants.featuresPerLayoutPerAxis,
                startY * constants.featuresPerLayoutPerAxis, 
                mapWidth * constants.featuresPerLayoutPerAxis, 
                mapHeight * constants.featuresPerLayoutPerAxis);

            float[,] mountainFeaturesLayoutNoise = RangeSubset(mountainFeaturesNoiseMap, 
                startX * constants.featuresPerLayoutPerAxis, 
                startY * constants.featuresPerLayoutPerAxis, 
                mapWidth * constants.featuresPerLayoutPerAxis, 
                mapHeight * constants.featuresPerLayoutPerAxis);

            Map map = mapGenerator.GenerateMap(container, mapLayoutNoise, groundFeaturesLayoutNoise, mountainFeaturesLayoutNoise); container.setMap(map);
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
