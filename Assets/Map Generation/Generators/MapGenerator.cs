using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode { NoiseMap, ColorMap, Mesh }
    public DrawMode drawMode;

    [Range(0, 1)]
    public float groundFeaturesImpactOnLayout;

    [Range(0, 2)]
    public float mountainFeaturesImpactOnLayout;

    public MapContainer demoMapContainer;

    public NoiseData mountainMutatorNoiseData;
    public NoiseData groundMutatorMapNoiseData;

    public NoiseData layoutMapNoiseData;
    public NoiseData groundFeaturesMapNoiseData;
    public NoiseData mountainFeaturesMapNoiseData;

    const int seedCap = 1000;
    System.Random rnd = new System.Random();

    private void RandomizeSeed() {
        rnd = new System.Random(System.Guid.NewGuid().GetHashCode());

        layoutMapNoiseData.seed = rnd.Next(1, seedCap);

        groundMutatorMapNoiseData.seed = rnd.Next(1, seedCap);
        mountainMutatorNoiseData.seed = rnd.Next(1, seedCap);

        groundFeaturesMapNoiseData.seed = rnd.Next(1, seedCap);
        mountainFeaturesMapNoiseData.seed = rnd.Next(1, seedCap);
    }

    /*
     * Interface Data
     * */

    public float[,] GenerateLayoutMap(int width, int length) {
        return NoiseGenerator.GenerateNoiseMap(width, length, layoutMapNoiseData);
    }

    public float[,] GenerateGroundMutatorMap(int width, int length) {
        float[,] map = NoiseGenerator.GenerateNoiseMap(width, length, groundMutatorMapNoiseData);
        NoiseGenerator.MinMaxofNormalize minMax = NoiseGenerator.NormalizeMap(map);

        return map;
    }

    public float[,] GenerateMountainMutatorMap(int width, int length) {
        float[,] map = NoiseGenerator.GenerateNoiseMap(width, length, mountainMutatorNoiseData);
        NoiseGenerator.MinMaxofNormalize minMax = NoiseGenerator.NormalizeMap(map);

        return map;
    }

    public float[,] GenerateGroundFeaturesMap(int width, int length) {
        layoutMapNoiseData.seed = rnd.Next(1, seedCap);

        return NoiseGenerator.GenerateNoiseMap(width, length, groundFeaturesMapNoiseData);
    }

    public float[,] GenerateMountainFeaturesMap(int width, int length) {
        return NoiseGenerator.GenerateNoiseMap(width, length, mountainFeaturesMapNoiseData);
    }

    /*
     * World Generation (series of maps)
     * */

    private static T[,] RangeSubset<T>(T[,] array, int startIndexX, int startIndexY, int lengthX, int lengthY) {
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
    const int suitableCoordinateDistance = 1;
    public LayoutCoordinate spawnCoordinate { get; private set; }

    // returns false if unable to get appropriate coordinate
    private bool GetSpawnCoordinate(float[,] layoutNoiseMap) {
        int midX = (layoutNoiseMap.GetLength(0) / 2) - 1;
        int midY = (layoutNoiseMap.GetLength(1) / 2) - 1;

        TerrainManager manager = Script.Get<TerrainManager>();

        for(int x = -suitableCoordinateDistance; x <= suitableCoordinateDistance; x++) {
            for(int y = -suitableCoordinateDistance; y <= suitableCoordinateDistance; y++) {
                float sample = layoutNoiseMap[midX + x, midY + y];

                if(manager.RegionTypeForValue(sample).type == RegionType.Type.Land) {
                    spawnCoordX = midX + x;
                    spawnCoordY = midY + y;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsLayoutSuitable(float[,] layoutNoiseMap) {
        if(GetSpawnCoordinate(layoutNoiseMap) == false) {
            return false;
        }

        return true;
    }


    /*
     * Entire map features and data
     * */

    // In layoutCoordinate space
    int mapLayoutWidth;
    int mapLayoutHeight;

    int totalLayoutWidth;
    int totalLayoutHeight;

    // In MapCoordinate space
    int mapFullWidth;
    int mapFullHeight;

    int totalFullWidth;
    int totalFullHeight;

    // Final noise output
    float[,] layoutNoiseMap;
    TerrainType[,] terrainMap;

    float[,] groundFeaturesNoiseMap;
    float[,] mountainFeaturesNoiseMap;

    float[,] finalNoiseMap;

    NoiseGenerator.MinMaxofNormalize minMaxOfFinalMap;

    class NoiseSubset {
        public float[,] layoutNoiseMap;
        public  TerrainType[,] terrainMap;

        public float[,] groundFeaturesNoiseMap;
        public float[,] mountainFeaturesNoiseMap;

        public float[,] finalNoiseMap;
    }

    // Generate the series of maps, returning a suitable spawn coordinate
    public void GenerateWorldStepOne(int mapCountX, int mapCountY) {
        MapsManager mapsManager = Script.Get<MapsManager>();
        Constants constants = Script.Get<Constants>();

        mapsManager.InitializeMaps(mapCountX, mapCountY);

        /*
         * Generate Layout 
         * */

        // In layoutCoordinate space
        mapLayoutWidth = constants.layoutMapWidth;
        mapLayoutHeight = constants.layoutMapHeight;

        totalLayoutWidth = mapLayoutWidth * constants.mapCountX;
        totalLayoutHeight = mapLayoutHeight * constants.mapCountY;

        layoutNoiseMap = new float[0, 0];
        bool success = false;

        while(success == false) {
            RandomizeSeed();
            layoutNoiseMap = GenerateLayoutMap(totalLayoutWidth, totalLayoutHeight);
            success = IsLayoutSuitable(layoutNoiseMap);
        }

        print("Found Suitable Map");

        float[,] groundMutatorMap = GenerateGroundMutatorMap(totalLayoutWidth, totalLayoutHeight);
        float[,] mountainMutatorMap = GenerateMountainMutatorMap(totalLayoutWidth, totalLayoutHeight);

        TerrainManager terrainManager = Script.Get<TerrainManager>();
        terrainManager.SetGroundMutatorMap(groundMutatorMap);
        terrainManager.SetMounainMutatorMap(mountainMutatorMap);

        terrainMap = PlateauMap(layoutNoiseMap);

        /*
         * Generate Details
         * */

        // In MapCoordinate space
        mapFullWidth = mapLayoutWidth * constants.featuresPerLayoutPerAxis;
        mapFullHeight = mapLayoutHeight * constants.featuresPerLayoutPerAxis;

        totalFullWidth = mapFullWidth * constants.mapCountX;
        totalFullHeight = mapFullHeight * constants.mapCountY;

        // Final Noise Generation
        groundFeaturesNoiseMap = GenerateGroundFeaturesMap(totalFullWidth, totalFullHeight);
        mountainFeaturesNoiseMap = GenerateMountainFeaturesMap(totalFullWidth, totalFullHeight);

        finalNoiseMap = CreateMapWithFeatures();

        minMaxOfFinalMap = NoiseGenerator.NormalizeMap(finalNoiseMap);

        spawnCoordinate = new LayoutCoordinate(0, 0, mapsManager.mapContainers[0]);
    }

    public int GenerateStepTwoCount() {
        MapsManager mapsManager = Script.Get<MapsManager>();
        return mapsManager.mapContainers.Count;
    }

    // Setup world in fragments
    public void GenerateWorldStepTwo(int iteration) {
        MapsManager mapsManager = Script.Get<MapsManager>();

        MapContainer container = mapsManager.mapContainers[iteration];

        int startXLayout = container.mapX * mapLayoutWidth;
        int startYLayout = container.mapY * mapLayoutHeight;

        if(spawnCoordX >= startXLayout && spawnCoordX < startXLayout + mapLayoutWidth && spawnCoordY >= startYLayout && spawnCoordY < startYLayout + mapLayoutHeight) {
            spawnCoordinate = new LayoutCoordinate(spawnCoordX - startXLayout, spawnCoordY - startYLayout, container);
        }

        NoiseSubset noiseSubset = GetNoiseSubsetForMap(container);

        GameObject mapOject = new GameObject("Map of " + container.name);
        Map map = mapOject.AddComponent<Map>();

        map.InitMap(noiseSubset.terrainMap, MeshGenerator.GenerateTerrainMesh(noiseSubset.finalNoiseMap));

        container.setMap(map);
    }

    public void GenerateWorldStepThree() {
        MapsManager mapsManager = Script.Get<MapsManager>();

        // Second pass to fill in overhang
        foreach(MapContainer container in mapsManager.mapContainers) {
            container.UpdateMapOverhang();
        }
    }

    private NoiseSubset GetNoiseSubsetForMap(MapContainer mapContainer) {
        Constants constants = Script.Get<Constants>();

        NoiseSubset noiseSubset = new NoiseSubset();

        int startXLayout = mapContainer.mapX * mapLayoutWidth;
        int startYLayout = mapContainer.mapY * mapLayoutHeight;

        noiseSubset.layoutNoiseMap = RangeSubset(layoutNoiseMap, startXLayout, startYLayout, mapLayoutWidth, mapLayoutHeight);
        noiseSubset.terrainMap = RangeSubset(terrainMap, startXLayout, startYLayout, mapLayoutWidth, mapLayoutHeight);

        int startXFull = startXLayout * constants.featuresPerLayoutPerAxis;
        int startYFull = startYLayout * constants.featuresPerLayoutPerAxis;

        noiseSubset.groundFeaturesNoiseMap = RangeSubset(groundFeaturesNoiseMap, startXFull, startYFull, mapFullWidth, mapFullHeight);
        noiseSubset.mountainFeaturesNoiseMap = RangeSubset(mountainFeaturesNoiseMap, startXFull, startYFull, mapFullWidth, mapFullHeight);
        noiseSubset.finalNoiseMap = RangeSubset(finalNoiseMap, startXFull, startYFull, mapFullWidth, mapFullHeight);

        return noiseSubset;
    }

    // Returns the height in MAP COORDINATE position
    public float GetHeightAt(MapCoordinate mapCoordinate) {
        NoiseSubset noiseSubset = GetNoiseSubsetForMap(mapCoordinate.mapContainer);
        return noiseSubset.finalNoiseMap[mapCoordinate.xAverageSample, mapCoordinate.yAverageSample];
    }

    public TerrainType GetTerrainAt(LayoutCoordinate layoutCoordinate) {
        Constants constants = Script.Get<Constants>();

        int startX = layoutCoordinate.mapContainer.mapX * constants.layoutMapWidth;
        int startY = layoutCoordinate.mapContainer.mapY * constants.layoutMapHeight;

        return terrainMap[startX + layoutCoordinate.x, startY + layoutCoordinate.y];
    }

    // Only call from Map
    public void UpdateTerrainAt(LayoutCoordinate layoutCoordinate, TerrainType terrainType) {
        Constants constants = Script.Get<Constants>();

        int startX = layoutCoordinate.mapContainer.mapX * constants.layoutMapWidth;
        int startY = layoutCoordinate.mapContainer.mapY * constants.layoutMapHeight;

        terrainMap[startX + layoutCoordinate.x, startY + layoutCoordinate.y] = terrainType;
    }

    /*
        * Map Modification
        * 
        * MUTATING FUNCTION: layoutNoiseMap, finalNoiseMap
        * */

    public float[,] TerraformHeightMap(TerraformTarget terraformTarget) {
        Constants constants = Script.Get<Constants>();

        LayoutCoordinate layoutCoordinate = terraformTarget.coordinate;
        MapContainer mapContainer = layoutCoordinate.mapContainer;

        int startXLayout = mapContainer.mapX * mapLayoutWidth;
        int startYLayout = mapContainer.mapY * mapLayoutHeight;

        int startXMapCoordinate = mapContainer.mapX * mapFullWidth;
        int startYMapCoordinate = mapContainer.mapY * mapFullHeight;

        MapCoordinate[,] coordinates = MapCoordinate.MapCoordinatesFromLayoutCoordinate(layoutCoordinate);

        RegionType terraformRegionType = Script.Get<TerrainManager>().regionTypeMap[terraformTarget.terrainTypeTarget.regionType];
        float heightAtNewRegion = HeightAtRegion(terraformRegionType);

        if(terraformTarget.initialHeight == null || terraformTarget.heightTarget == null) {
            
            terraformTarget.initialHeight = new float[constants.featuresPerLayoutPerAxis, constants.featuresPerLayoutPerAxis];
            terraformTarget.heightTarget = new float[constants.featuresPerLayoutPerAxis, constants.featuresPerLayoutPerAxis];

            for(int width = 0; width < coordinates.GetLength(0); width++) {
                for(int height = 0; height < coordinates.GetLength(1); height++) {
                    MapCoordinate mapCoordinate = coordinates[width, height];

                    terraformTarget.initialHeight[width, height] = GetHeightAt(mapCoordinate);

                    int x = startXMapCoordinate + mapCoordinate.xLowSample;
                    int y = startYMapCoordinate + mapCoordinate.yLowSample;

                    float finalValue = MapAtXYWithFeatures(x, y, terraformTarget.terrainTypeTarget, heightAtNewRegion);
                    terraformTarget.heightTarget[width, height] = Mathf.InverseLerp(minMaxOfFinalMap.min, minMaxOfFinalMap.max, finalValue);
                }
            }
        }

        for(int width = 0; width < coordinates.GetLength(0); width++) {
            for(int height = 0; height < coordinates.GetLength(1); height++) {
                MapCoordinate mapCoordinate = coordinates[width, height];

                int x = startXMapCoordinate + mapCoordinate.xLowSample;
                int y = startYMapCoordinate + mapCoordinate.yLowSample;

                //float finalValue = MapAtXYWithFeatures(x, y, terraformTarget.terrainTypeTarget, heightAtNewRegion);
                finalNoiseMap[x, y] = Mathf.Lerp(terraformTarget.initialHeight[width, height], terraformTarget.heightTarget[width, height], terraformTarget.percentage);
            }
        }

        // Upon completion of terraform, update out layout map to represent what we have 
        if (terraformTarget.percentage == 1) {
            layoutNoiseMap[startXLayout + layoutCoordinate.x, startYLayout + layoutCoordinate.y] = heightAtNewRegion;       
        }

        return GetNoiseSubsetForMap(mapContainer).finalNoiseMap;        
    }

    /*
     * Map (2d array) Creation
     * */

    // Returns a 2d array of terrainTypes
    public TerrainType[,] PlateauMap(float[,] map) {
        int mapWidth = map.GetLength(0);
        int mapHeight = map.GetLength(1);

        TerrainType[,] terrainMap = new TerrainType[map.GetLength(0), map.GetLength(1)];
        TerrainManager terrainManager = Script.Get<TerrainManager>();

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {

                RegionType region = terrainManager.RegionTypeForValue(map[x, y]);

                terrainMap[x, y] = terrainManager.TerrainTypeForRegion(region, x, y);
                map[x, y] = HeightAtRegion(region);
            }
        }

        return terrainMap;
    }

    private float HeightAtRegion(RegionType region) {
        return (region.plateauAtBase ? region.noiseBase + 0.001f : region.noiseMax - 0.001f);
    }

    // Given arrays for our layouts, features and terrain types, create a final height map
    private float[,] CreateMapWithFeatures() {
        //int featuresWidth = groundFeaturesNoiseMap.GetLength(0);
        //int featuresHeight = groundFeaturesNoiseMap.GetLength(1);

        float[,] fullMap = new float[totalFullWidth, totalFullHeight];
        for(int y = 0; y < totalFullWidth; y++) {
            for(int x = 0; x < totalFullHeight; x++) {
                fullMap[x,y] = MapAtXYWithFeatures(x, y);
            }
        }

        return fullMap;
    }

    // On MapCoordinate scale, but for the entire generation, not a single mapContainer
    private float MapAtXYWithFeatures(int x, int y, TerrainType? withAlternateTerrain = null, float? withAlternateLayoutValue = null) {
        Constants constants = Script.Get<Constants>();
        int featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;

        int dipRadius = 3;

        int sampleX = x / featuresPerLayoutPerAxis;
        int sampleY = y / featuresPerLayoutPerAxis;

        TerrainType thisTerrainType = terrainMap[sampleX, sampleY];

        if (withAlternateTerrain != null) {
            thisTerrainType = withAlternateTerrain.Value;
        }

        float mapAtXY = SampleAtXY(x, y, withAlternateTerrain, withAlternateLayoutValue);

        float distanceToEdgeX = x % featuresPerLayoutPerAxis;
        float distanceToEdgeY = y % featuresPerLayoutPerAxis;

        for(int i = 0; i < dipRadius; i++) {

            float baseline = Script.Get<TerrainManager>().regionTypeMap[thisTerrainType.regionType].noiseBase * (dipRadius - (i + 1)) / dipRadius;

            // Left
            if((sampleX == 0) || (sampleX - 1 >= 0 && thisTerrainType.priority > terrainMap[sampleX - 1, sampleY].priority)) {
                if(distanceToEdgeX == i) {
                    mapAtXY = baseline + (mapAtXY * ((i + 1)) / dipRadius);
                    continue;
                }
            }

            // Top
            if((sampleY == 0) || (sampleY - 1 >= 0 && thisTerrainType.priority > terrainMap[sampleX, sampleY - 1].priority)) {
                if(distanceToEdgeY == i) {
                    mapAtXY = baseline + (mapAtXY * ((i + 1)) / dipRadius);
                    continue;
                }
            }

            // Right
            if((sampleX + 1 == terrainMap.GetLength(0)) || (sampleX + 1 < terrainMap.GetLength(0) && thisTerrainType.priority > terrainMap[sampleX + 1, sampleY].priority)) {
                if((featuresPerLayoutPerAxis - distanceToEdgeX - 1) == i) {
                    mapAtXY = baseline + (mapAtXY * ((i + 1)) / dipRadius);
                    continue;
                }
            }

            //// Bottom
            if((sampleY + 1 == terrainMap.GetLength(1)) || (sampleY + 1 < terrainMap.GetLength(1) && thisTerrainType.priority > terrainMap[sampleX, sampleY + 1].priority)) {
                if(featuresPerLayoutPerAxis - distanceToEdgeY - 1 == i) {
                    mapAtXY = baseline + (mapAtXY * ((i + 1)) / dipRadius);
                    continue;
                }
            }
        }


        return mapAtXY;

        //if (distanceToEdgeX == 0 || (distanceToEdgeX == featuresPerLayoutPerAxis) || distanceToEdgeY == 0 || (distanceToEdgeY == featuresPerLayoutPerAxis)) {
        //    fullMap[x, y] *= 0.30f;
        //}

        //if(distanceToEdgeX == 1 || (distanceToEdgeX == featuresPerLayoutPerAxis - 1) || distanceToEdgeY == 1 || (distanceToEdgeY == featuresPerLayoutPerAxis - 1)) {
        //    fullMap[x, y] *= 0.60f;
        //}

        //if(distanceToEdgeX == 2 || (distanceToEdgeX == featuresPerLayoutPerAxis - 2) || distanceToEdgeY == 2 || (distanceToEdgeY == featuresPerLayoutPerAxis - 2)) {
        //    fullMap[x, y] *= 0.90f;
        //}
    }

    // Sampling at an index for CreateMapWithFeatures
    private float SampleAtXY(int x, int y, TerrainType? withAlternateTerrain = null, float ? withAlternateLayoutValue = null) {
        Constants constants = Script.Get<Constants>();
        int featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;

        int sampleX = x / featuresPerLayoutPerAxis;
        int sampleY = y / featuresPerLayoutPerAxis;

        float sampleValue = layoutNoiseMap[sampleX, sampleY];
        TerrainType terrainType = terrainMap[sampleX, sampleY];


        if (withAlternateTerrain != null) {
            terrainType = withAlternateTerrain.Value;
        }

        if (withAlternateLayoutValue != null) {
            sampleValue = withAlternateLayoutValue.Value;
        }

        switch(terrainType.regionType) {
            case RegionType.Type.Water:
                return sampleValue;
            case RegionType.Type.Land:
                return (sampleValue) + ((groundFeaturesNoiseMap[x, y] * groundFeaturesImpactOnLayout) - (1 * groundFeaturesImpactOnLayout) / 2f);
            case RegionType.Type.Mountain:
                return sampleValue + (mountainFeaturesNoiseMap[x, y] * mountainFeaturesImpactOnLayout);
        }

        return 0;
    }

    /*
     * Creating color maps from finished noise maps 
     * */

    private Color[] CreateColorMap(float[,] noiseMap) {

        int noiseMapWidth = noiseMap.GetLength(0);
        int noiseMapHeight = noiseMap.GetLength(1);

        TerrainType[] regions = Script.Get<TerrainManager>().terrainTypes;

        Color[] colorMap = new Color[noiseMapWidth * noiseMapHeight];
        for(int y = 0; y < noiseMapHeight; y++) {
            for(int x = 0; x < noiseMapWidth; x++) {

                float currentHeight = noiseMap[x, y];
                RegionType region = Script.Get<TerrainManager>().RegionTypeForValue(currentHeight);
                colorMap[y * noiseMapWidth + x] = region.color;
            }
        }

        return colorMap;
    }

    private Color[] CreateColorMapWithTerrain(float[,] noiseMap, TerrainType[,] terrainTypeMap, MapContainer mapContainer) {
        Constants constants = Script.Get<Constants>();
        int featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;

        int noiseMapWidth = noiseMap.GetLength(0);
        int noiseMapHeight = noiseMap.GetLength(1);

        //  + 2 for overhang
        int colorMapWidth = noiseMapWidth + 2;
        int colorMapHeight = noiseMapHeight + 2;

        int terrainMapWidth = terrainTypeMap.GetLength(0);
        int terrainMapHeight = terrainTypeMap.GetLength(1);

        Color[] colorMap = new Color[(colorMapWidth) * (colorMapHeight)];
        for(int y = 0; y < colorMapWidth; y++) {
            for(int x = 0; x < colorMapHeight; x++) {

                int boundedX = x - 1;
                int boundedY = y - 1;

                Color colorAtIndex = Color.white;
                if(!(boundedX < 0 || boundedX > noiseMapWidth - 1 || boundedY < 0 || boundedY > noiseMapHeight - 1)) {                   
                    int finalSampleX = boundedX / (noiseMapWidth / terrainMapWidth);
                    int finalSampleY = boundedY / (noiseMapHeight / terrainMapHeight);

                    colorAtIndex = terrainTypeMap[finalSampleX, finalSampleY].color;

                    if((boundedX + 1) % featuresPerLayoutPerAxis == 0 || (boundedY + 1) % featuresPerLayoutPerAxis == 0) {
                        // If the next terrain is higher, use that color index instead
                        if((boundedX + 1) % featuresPerLayoutPerAxis == 0 && (finalSampleX + 1) < terrainTypeMap.GetLength(1) - 1) {
                            if(terrainTypeMap[finalSampleX + 1, finalSampleY].priority > terrainTypeMap[finalSampleX, finalSampleY].priority) {
                                colorAtIndex = terrainTypeMap[finalSampleX + 1, finalSampleY].color;
                            }
                        }

                        if ((boundedY + 1) % featuresPerLayoutPerAxis == 0 && (finalSampleY + 1) < terrainTypeMap.GetLength(1) - 1) {
                            if (terrainTypeMap[finalSampleX, finalSampleY + 1].priority > terrainTypeMap[finalSampleX, finalSampleY].priority) {
                                colorAtIndex = terrainTypeMap[finalSampleX, finalSampleY + 1].color;
                            }
                        }

                        colorAtIndex *= 2;
                    }
                }

                colorMap[y * colorMapWidth + x] = colorAtIndex;
            }
        }

        return colorMap;
    }

    /*
     * Debug
     * */

    public void DisplayDebugMap(Map map) {
        /* MapDebugDisplay debugDisplay = FindObjectOfType<MapDebugDisplay>();
         TextureGenerator textureGenerator = Script.Get<TextureGenerator>();

         switch(drawMode) {
             case DrawMode.NoiseMap:
                 if(debugDisplay != null) {
                     debugDisplay.DrawTexture(textureGenerator.TextureFromNoiseMap(map.noiseMap));
                     debugDisplay.DrawTextures(textureGenerator.TextureFromNoiseMap(layoutNoiseMap), textureGenerator.TextureFromNoiseMap(groundFeaturesNoiseMap));
                 }
                 break;
             case DrawMode.ColorMap:
                 if(debugDisplay != null) {

                     debugDisplay.DrawTexture(textureGenerator.TextureFromColorMap(CreateColorMap(noiseMap), noiseMap.GetLength(0), noiseMap.GetLength(1)));
                     debugDisplay.DrawTextures(textureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                         textureGenerator.TextureFromColorMap(CreateColorMap(groundFeaturesNoiseMap), groundFeaturesNoiseMap.GetLength(0), groundFeaturesNoiseMap.GetLength(1))
                         );
                 }
                 break;
             case DrawMode.Mesh:
                 mapContainer.setMap(map, false);

                 if(debugDisplay != null) {
                     debugDisplay.DrawMeshes(MeshGenerator.GenerateTerrainMesh(layoutNoiseMap, 1), textureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                     MeshGenerator.GenerateTerrainMesh(groundFeaturesNoiseMap, 1), textureGenerator.TextureFromColorMap(CreateColorMap(groundFeaturesNoiseMap), groundFeaturesNoiseMap.GetLength(0), groundFeaturesNoiseMap.GetLength(1))
                     );
                 }
                 break;
         }*/
    }
}
