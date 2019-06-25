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

    int spawnCoordX;
    int spawnCoordY;
    LayoutCoordinate spawnCoordinate;
    const int suitableCoordinateDistance = 1;

    // returns false if unable to get appropriate coordinate
    private bool GetSpawnCoordinate(float[,] layoutNoiseMap) {
        int midX = (layoutNoiseMap.GetLength(0) / 2) - 1;
        int midY = (layoutNoiseMap.GetLength(1) / 2) - 1;

        TerrainType land = mapGenerator.TerrainForRegion(RegionType.Land);

        for (int x = -suitableCoordinateDistance; x <= suitableCoordinateDistance; x++) {
            for(int y = -suitableCoordinateDistance; y <= suitableCoordinateDistance; y++) {
                float sample = layoutNoiseMap[midX + x, midY + y];

                if(land.ValueIsMember(sample)) {
                    spawnCoordX = midX + x;
                    spawnCoordY = midY + y;
                    return true;
                }
            }
        } 

        return false;
    }

    private bool IsLayoutSuitable(float[,] layoutNoiseMap) {        
        if (GetSpawnCoordinate(layoutNoiseMap) == false) {
            return false;
        }

        return true;
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

        float[,] layoutNoiseMap = new float[0,0];
        bool success = false;

        while(success == false) {
            layoutNoiseMap = mapGenerator.GenerateLayoutMap(totalWidth, totalHeight);
            success = IsLayoutSuitable(layoutNoiseMap);
        }               

        float[,] groundFeaturesNoiseMap = mapGenerator.GenerateGroundFeaturesMap(totalWidth * constants.featuresPerLayoutPerAxis, totalHeight * constants.featuresPerLayoutPerAxis);
        float[,] mountainFeaturesNoiseMap = mapGenerator.GenerateMountainFeaturesMap(totalWidth * constants.featuresPerLayoutPerAxis, totalHeight * constants.featuresPerLayoutPerAxis);


        // Setup world
        foreach(MapContainer container in mapsManager.mapContainers) {
            int startX = container.mapX * mapWidth;
            int startY = container.mapY * mapHeight;

            if (spawnCoordX >= startX && spawnCoordX < startX + mapWidth && spawnCoordY >= startY && spawnCoordY < startY + mapHeight) {
                spawnCoordinate = new LayoutCoordinate(spawnCoordX - startX, spawnCoordY - startY, container);
            }

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

            Map map = mapGenerator.GenerateMap(container, mapLayoutNoise, groundFeaturesLayoutNoise, mountainFeaturesLayoutNoise);
            container.setMap(map);
        }

        // Second pass to fill in overhang
        foreach(MapContainer container in mapsManager.mapContainers) {
            container.UpdateMapOverhang();
        }

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
