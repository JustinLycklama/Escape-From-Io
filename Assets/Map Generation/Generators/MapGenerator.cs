﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode { NoiseMap, ColorMap, Mesh }
    public DrawMode drawMode;

    [SerializeField]
    private PremadeNoiseGenerator premadeLayoutMap = null;

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
    System.Random rnd = NoiseGenerator.random;

    private void RandomizeSeed() {      
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
    private static Dictionary<int, List<float[,]>> resourceFloatPoolMap = new Dictionary<int, List<float[,]>>();
    private static Dictionary<int, int> resourceFloatPoolIndex = new Dictionary<int, int>();

    private static Dictionary<int, List<TerrainType[,]>> resourceTerrainTypePoolMap = new Dictionary<int, List<TerrainType[,]>>();
    private static Dictionary<int, int> resourceTerrainTypetPoolIndex = new Dictionary<int, int>();
    
    private static float[,] FloatResourcePool(int resourceLength) {
        return ResourcePool(resourceLength, resourceFloatPoolMap, resourceFloatPoolIndex);
    }

    private static TerrainType[,] TerrainResourcePool(int resourceLength) {
        return ResourcePool(resourceLength, resourceTerrainTypePoolMap, resourceTerrainTypetPoolIndex);
    }

    private static T[,] ResourcePool<T>(int resourceLength, Dictionary<int, List<T[,]>> pool, Dictionary<int, int> index) {
        if(!pool.ContainsKey(resourceLength)) {
            pool[resourceLength] = new List<T[,]>();
            index[resourceLength] = 0;
        }

        List<T[,]> poolList = pool[resourceLength];

        if (index[resourceLength] >= poolList.Count) {
            poolList.Add(new T[resourceLength, resourceLength]);
            pool[resourceLength] = poolList;
        }

        index[resourceLength]++;
        return poolList[index[resourceLength] - 1];
    }

    private static void ResetResourcePool() {
        foreach(int key in resourceFloatPoolMap.Keys.ToArray()) {
            resourceFloatPoolIndex[key] = 0;
        }

        foreach(int key in resourceTerrainTypePoolMap.Keys.ToArray()) {
            resourceTerrainTypetPoolIndex[key] = 0;
        }
    }

    // This is a method we call a LOT. Pool the array resources, to save on GC
    public static T[,] RangeSubset<T>(T[,] array, int startIndexX, int startIndexY, int lengthX, int lengthY) {
        T[,] subset;

        if(typeof(T) == typeof(float) && lengthX == lengthY) {
            subset = FloatResourcePool(lengthX) as T[,];
        } else if(typeof(T) == typeof(TerrainType) && lengthX == lengthY) {
            subset = TerrainResourcePool(lengthX) as T[,];
        } else {
            subset = new T[lengthX, lengthY];
        }        

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
        // Get Spawn Coordinate
        if(GetSpawnCoordinate(layoutNoiseMap) == false) {
            return false;
        }

        // There should be at least 1 walkable tile adjacent to spawn coordinate
        int groundTerrainCount = 0;
        TerrainManager manager = Script.Get<TerrainManager>();

        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++) {

                if (x == y || (x == -1 && y == 1) || (x == 1 && y == -1)) {
                    continue;
                }

                int sampleX = spawnCoordX + x;
                int sampleY = spawnCoordY + y;

                float sample = layoutNoiseMap[sampleX, sampleY];

                if(manager.RegionTypeForValue(sample).type == RegionType.Type.Land) {
                    groundTerrainCount++;
                }
            }
        }

        return groundTerrainCount >= 1;
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
    float[,] groundFeaturesNoiseMap;
    float[,] mountainFeaturesNoiseMap;

    float[,] layoutNoiseMap;
    TerrainType[,] terrainMap;

    float[,] finalNoiseMap;

    // Mutators for the original map - used for turning unknown tiles into starting tiles
    float[,] originalLayoutNoiseMap;
    TerrainType[,] originalLayoutTerrainMap;

    // Min Max based on the original map
    NoiseGenerator.MinMaxofNormalize minMaxOfFinalMap;

    class NoiseSubset {
        public float[,] layoutNoiseMap;
        public TerrainType[,] terrainMap;

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

            layoutNoiseMap = premadeLayoutMap?.LayoutNoiseData ?? GenerateLayoutMap(totalLayoutWidth, totalLayoutHeight);            
            success = IsLayoutSuitable(layoutNoiseMap);

            if (success == false && premadeLayoutMap?.LayoutNoiseData != null) {
                break;
            }
        }

        print("Found Suitable Map");

        float[,] groundMutatorMap = premadeLayoutMap?.GroundMutatorNoiseData ?? GenerateGroundMutatorMap(totalLayoutWidth, totalLayoutHeight);
        float[,] mountainMutatorMap = premadeLayoutMap?.MountainMutatorNoiseData ?? GenerateMountainMutatorMap(totalLayoutWidth, totalLayoutHeight);

        // If we are using a premade layout map, don't add random alunar rocks
        if (premadeLayoutMap?.LayoutNoiseData != null) {
            maxSavedCoordinateValues = 0;
        }

        TerrainManager terrainManager = Script.Get<TerrainManager>();
        terrainManager.SetGroundMutatorMap(groundMutatorMap);
        terrainManager.SetMounainMutatorMap(mountainMutatorMap);

        originalLayoutNoiseMap = (float[,])layoutNoiseMap.Clone();
        originalLayoutTerrainMap = PlateauLayoutMap(spawnCoordX, spawnCoordY);

        int terrainMapWidth = layoutNoiseMap.GetLength(0);
        int terrainMapHeight = layoutNoiseMap.GetLength(1);

        terrainMap = new TerrainType[terrainMapWidth, terrainMapHeight];
        for(int x = 0; x < terrainMapWidth; x++) {
            for(int y = 0; y < terrainMapHeight; y++) {
                terrainMap[x, y] = terrainManager.terrainTypeMap[TerrainType.Type.Unknown];
            }
        }

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

        finalNoiseMap = CreateMapWithFeatures(terrainMap);

        minMaxOfFinalMap = NoiseGenerator.MinMaxForMap(originalLayoutNoiseMap);

        NoiseGenerator.MinMaxofNormalize minMaxOfGround = NoiseGenerator.MinMaxForMap(groundFeaturesNoiseMap);
        NoiseGenerator.MinMaxofNormalize minMaxOfMountain = NoiseGenerator.MinMaxForMap(mountainFeaturesNoiseMap);

        minMaxOfFinalMap.min += minMaxOfGround.min * groundFeaturesImpactOnLayout * 2;
        minMaxOfFinalMap.min += minMaxOfMountain.min * mountainFeaturesImpactOnLayout;

        minMaxOfFinalMap.max += minMaxOfGround.max * groundFeaturesImpactOnLayout * 2;
        minMaxOfFinalMap.max += minMaxOfMountain.max * mountainFeaturesImpactOnLayout;

        //print("Min: " + minMaxOfFinalMap.min + "  Max: " + minMaxOfFinalMap.max);

        NoiseGenerator.NormalizeMapUsingMinMax(finalNoiseMap, minMaxOfFinalMap);

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
        ResetResourcePool();

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

    public TerrainType[,] GetTerrainForMap(MapContainer mapContainer) {
        int startXLayout = mapContainer.mapX * mapLayoutWidth;
        int startYLayout = mapContainer.mapY * mapLayoutHeight;

        return RangeSubset(terrainMap, startXLayout, startYLayout, mapLayoutWidth, mapLayoutHeight);
    }

    public float[,] GetFinalNoiseMap() {
        return finalNoiseMap;
    }

    // Returns the height in MAP COORDINATE position
    private KeyValuePair<MapContainer, float[,]> cachedMapContainer = new KeyValuePair<MapContainer, float[,]>();
    public float GetHeightAt(MapCoordinate mapCoordinate) {
        
        // We often request heights for the same mapContainer over and over.
        if (mapCoordinate.mapContainer != cachedMapContainer.Key) {
            NoiseSubset noiseSubset = GetNoiseSubsetForMap(mapCoordinate.mapContainer);
            cachedMapContainer = new KeyValuePair<MapContainer, float[,]>(mapCoordinate.mapContainer, noiseSubset.finalNoiseMap);

            ResetResourcePool();
        }

        return cachedMapContainer.Value[mapCoordinate.xAverageSample, mapCoordinate.yAverageSample];
    }

    private void ResetCachedMap() {
        cachedMapContainer = new KeyValuePair<MapContainer, float[,]>();        
    }

    public TerrainType GetTerrainAtAbsoluteXY(int x, int y) {
        return terrainMap[x, y];
    }

    public TerrainType GetTerrainAt(LayoutCoordinate layoutCoordinate) {
        Constants constants = Script.Get<Constants>();

        int startX = layoutCoordinate.mapContainer.mapX * constants.layoutMapWidth;
        int startY = layoutCoordinate.mapContainer.mapY * constants.layoutMapHeight;

        return GetTerrainAtAbsoluteXY(startX + layoutCoordinate.x, startY + layoutCoordinate.y);
    }

    // Only call from Map
    public void UpdateTerrainAt(LayoutCoordinate layoutCoordinate, TerrainType terrainType) {
        Constants constants = Script.Get<Constants>();

        int startX = layoutCoordinate.mapContainer.mapX * constants.layoutMapWidth;
        int startY = layoutCoordinate.mapContainer.mapY * constants.layoutMapHeight;

        int posX = startX + layoutCoordinate.x;
        int posY = startY + layoutCoordinate.y;

        if (terrainMap[posX, posY].type == TerrainType.Type.AlunarRock) {
            for(int i = 0; i < listOfLunarLocations.Count; i++) {
                KeyValuePair<int, int> pair = listOfLunarLocations[i];
                if (pair.Key == posX && pair.Value == posY) {
                    listOfLunarLocations.RemoveAt(i);
                    break;
                }
            }
        }

        terrainMap[posX, posY] = terrainType;        
    }

    /*
        * Map Modification
        * 
        * MUTATING FUNCTION: layoutNoiseMap, finalNoiseMap
        * originalLayoutNoiseMap, originalFinalNosieMap
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

                    float finalValue = MapAtXYWithFeatures(x, y, terrainMap, terraformTarget.terrainTypeTarget, heightAtNewRegion);

                    //if (finalValue < minMaxOfFinalMap.min || finalValue > minMaxOfFinalMap.max) {
                    //    //todo:
                    //    minMaxOfFinalMap = NoiseGenerator.NormalizeMap(finalNoiseMap);

                    //}

                    terraformTarget.heightTarget[width, height] = Mathf.InverseLerp(minMaxOfFinalMap.min, minMaxOfFinalMap.max, finalValue);
                }
            }
        }

        for(int width = 0; width < coordinates.GetLength(0); width++) {
            for(int height = 0; height < coordinates.GetLength(1); height++) {
                MapCoordinate mapCoordinate = coordinates[width, height];

                int x = startXMapCoordinate + mapCoordinate.xLowSample;
                int y = startYMapCoordinate + mapCoordinate.yLowSample;

                float finalValue = Mathf.Lerp(terraformTarget.initialHeight[width, height], terraformTarget.heightTarget[width, height], terraformTarget.percentage);

                finalNoiseMap[x, y] = finalValue;

                // Also update the original. The original will be used in places where we don't know the current height due to Unknown tiles, but if we update something, it should be known to the original as well
                //originalFinalNoiseMap[x, y] = finalValue;
            }
        }

        // Upon completion of terraform, update out layout map to represent what we have 
        if(terraformTarget.percentage == 1) {
            layoutNoiseMap[startXLayout + layoutCoordinate.x, startYLayout + layoutCoordinate.y] = heightAtNewRegion;
            //originalLayoutNoiseMap[startXLayout + layoutCoordinate.x, startYLayout + layoutCoordinate.y] = heightAtNewRegion;
        }

        float[,] finalSubsetNoiseMap = GetNoiseSubsetForMap(mapContainer).finalNoiseMap;
        ResetResourcePool();

        ResetCachedMap();

        return finalSubsetNoiseMap;
    }

    /*
     * Map (2d array) Creation
     * */

    // Used for inserting (Azure) Alunar Rocks after map noise generation
    int maxSavedCoordinateValues = 30;
    const int invalidMutatorValue = 20;
    struct MutatorCoordinateValues {
        public float mutator;
        public int x, y;

        public MutatorCoordinateValues(float mutator, int x, int y) {
            this.mutator = mutator;
            this.x = x;
            this.y = y;
        }
    }

    public List<KeyValuePair<int, int>> listOfLunarLocations = new List<KeyValuePair<int, int>>();

    // Returns a 2d array of terrainTypes
    public TerrainType[,] PlateauLayoutMap(int spawnX, int spawnY) {
        int mapWidth = layoutNoiseMap.GetLength(0);
        int mapHeight = layoutNoiseMap.GetLength(1);

        TerrainType[,] terrainMap = new TerrainType[mapWidth, mapHeight];
        TerrainManager terrainManager = Script.Get<TerrainManager>();

        MutatorCoordinateValues[] coordinateValues = new MutatorCoordinateValues[maxSavedCoordinateValues];

         Vector2 spawnCoord = new Vector2(spawnX, spawnY);

        for(int i = 0; i < maxSavedCoordinateValues; i ++) {
            coordinateValues[i] = new MutatorCoordinateValues(invalidMutatorValue, -1, -1);
        }

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {

                RegionType region = terrainManager.RegionTypeForValue(layoutNoiseMap[x, y]);

                float mutatorValue;

                terrainMap[x, y] = terrainManager.TerrainTypeForRegion(region, x, y, out mutatorValue);
                layoutNoiseMap[x, y] = HeightAtRegion(region);

                if (maxSavedCoordinateValues > 0 && region.type == RegionType.Type.Mountain && mutatorValue < coordinateValues[maxSavedCoordinateValues - 1].mutator) {
                    if (Vector2.Distance(spawnCoord, new Vector2(x, y)) > 3) {
                        coordinateValues[maxSavedCoordinateValues - 1] = new MutatorCoordinateValues(mutatorValue, x, y);
                        coordinateValues = coordinateValues.OrderBy(m => m.mutator).ToArray();
                    }
                }                
            }
        }

        foreach(MutatorCoordinateValues values in coordinateValues) {
            if (values.mutator == invalidMutatorValue) {
                continue;
            }

            terrainMap[values.x, values.y] = terrainManager.terrainTypeMap[TerrainType.Type.AlunarRock];
            listOfLunarLocations.Add(new KeyValuePair<int, int>(values.x, values.y));
        }

        return terrainMap;
    }

    public TerrainType KnownTerrainTypeAtIndex(int x, int y) {
        return originalLayoutTerrainMap[x, y];
    }

    private float HeightAtRegion(RegionType region) {
        return (region.plateauAtBase ? region.noiseBase + 0.001f : region.noiseMax - 0.001f);
    }

    // Given arrays for our layouts, features and terrain types, create a final height map
    private float[,] CreateMapWithFeatures(TerrainType[,] usingTerrainMap) {
        //int featuresWidth = groundFeaturesNoiseMap.GetLength(0);
        //int featuresHeight = groundFeaturesNoiseMap.GetLength(1);

        float[,] fullMap = new float[totalFullWidth, totalFullHeight];
        for(int y = 0; y < totalFullWidth; y++) {
            for(int x = 0; x < totalFullHeight; x++) {
                fullMap[x,y] = MapAtXYWithFeatures(x, y, usingTerrainMap);
            }
        }

        return fullMap;
    }

    // On MapCoordinate scale, but for the entire generation, not a single mapContainer
    private float MapAtXYWithFeatures(int x, int y, TerrainType[,] usingTerrainMap, TerrainType? withAlternateTerrain = null, float? withAlternateLayoutValue = null) {
        Constants constants = Script.Get<Constants>();

        Func<int, int, TerrainType> TerrainAtLocation = (subX, subY) => {

            TerrainType currentTerrainType = usingTerrainMap[subX, subY];

            if(currentTerrainType.regionType == RegionType.Type.Unknown) {
                // If the current type is unknown, use what the original value will be
                currentTerrainType = originalLayoutTerrainMap[subX, subY];
            }

            return currentTerrainType;
        };

        int featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;

        int dipRadius = 3;

        int sampleX = x / featuresPerLayoutPerAxis;
        int sampleY = y / featuresPerLayoutPerAxis;

        TerrainType thisTerrainType = usingTerrainMap[sampleX, sampleY];

        if (withAlternateTerrain != null) {
            thisTerrainType = withAlternateTerrain.Value;
        }

        float mapAtXY = SampleAtXY(x, y, thisTerrainType, withAlternateLayoutValue);

        float distanceToEdgeX = x % featuresPerLayoutPerAxis;
        float distanceToEdgeY = y % featuresPerLayoutPerAxis;

        for(int i = 0; i < dipRadius; i++) {

            float baseline = Script.Get<TerrainManager>().regionTypeMap[thisTerrainType.regionType].noiseBase * (dipRadius - (i + 1)) / dipRadius;

            // Left
            if((sampleX == 0) || (sampleX - 1 >= 0 && thisTerrainType.priority > TerrainAtLocation(sampleX - 1, sampleY).priority)) {
                if(distanceToEdgeX == i) {
                    mapAtXY = baseline + (mapAtXY * ((i + 1)) / dipRadius);
                    continue;
                }
            }

            // Top
            if((sampleY == 0) || (sampleY - 1 >= 0 && thisTerrainType.priority > TerrainAtLocation(sampleX, sampleY - 1).priority)) {
                if(distanceToEdgeY == i) {
                    mapAtXY = baseline + (mapAtXY * ((i + 1)) / dipRadius);
                    continue;
                }
            }

            // Right
            if((sampleX + 1 == usingTerrainMap.GetLength(0)) || (sampleX + 1 < usingTerrainMap.GetLength(0) && thisTerrainType.priority > TerrainAtLocation(sampleX + 1, sampleY).priority)) {
                if((featuresPerLayoutPerAxis - distanceToEdgeX - 1) == i) {
                    mapAtXY = baseline + (mapAtXY * ((i + 1)) / dipRadius);
                    continue;
                }
            }

            //// Bottom
            if((sampleY + 1 == usingTerrainMap.GetLength(1)) || (sampleY + 1 < usingTerrainMap.GetLength(1) && thisTerrainType.priority > TerrainAtLocation(sampleX, sampleY + 1).priority)) {
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
                return (sampleValue) + ((groundFeaturesNoiseMap[x, y] * groundFeaturesImpactOnLayout * 2) - (groundFeaturesImpactOnLayout));
            case RegionType.Type.Mountain:
                return sampleValue + (mountainFeaturesNoiseMap[x, y] * mountainFeaturesImpactOnLayout);
        }

        return 0;
    }

    /*
     * Creating color maps from finished noise maps 
     * */

    //private Color[] CreateColorMap(float[,] noiseMap) {

    //    int noiseMapWidth = noiseMap.GetLength(0);
    //    int noiseMapHeight = noiseMap.GetLength(1);

    //    TerrainType[] regions = Script.Get<TerrainManager>().terrainTypes;

    //    Color[] colorMap = new Color[noiseMapWidth * noiseMapHeight];
    //    for(int y = 0; y < noiseMapHeight; y++) {
    //        for(int x = 0; x < noiseMapWidth; x++) {

    //            float currentHeight = noiseMap[x, y];
    //            RegionType region = Script.Get<TerrainManager>().RegionTypeForValue(currentHeight);
    //            colorMap[y * noiseMapWidth + x] = region.color;
    //        }
    //    }

    //    return colorMap;
    //}

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
