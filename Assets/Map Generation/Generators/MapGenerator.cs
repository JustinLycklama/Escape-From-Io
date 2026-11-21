using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class MapGenerator : MonoBehaviour {

    // Awake runs before any Start, so every system that caches NoiseGenerator.random (e.g. EnemyManager) gets this game's seeded instance
    void Awake() {
        NoiseGenerator.ResetRandom();
    }

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
    // Read through each time: NoiseGenerator.ResetRandom (MapGenerator.Awake) replaces the instance, and a field captured at
    // construction kept the previous stream, so the logged seed did not reproduce the layout (#33)
    System.Random rnd => NoiseGenerator.random;

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

        // Heights come from the terrain each tile will be once revealed, not from Unknown (height 0). Unexplored tiles still
        // render with the Unknown texture, but revealed edges now meet the real neighbouring height instead of a cliff to 0 (#28)
        finalNoiseMap = CreateMapWithFeatures(originalLayoutTerrainMap);

        minMaxOfFinalMap = NoiseGenerator.MinMaxForMap(originalLayoutNoiseMap);

        NoiseGenerator.MinMaxofNormalize minMaxOfGround = NoiseGenerator.MinMaxForMap(groundFeaturesNoiseMap);
        NoiseGenerator.MinMaxofNormalize minMaxOfMountain = NoiseGenerator.MinMaxForMap(mountainFeaturesNoiseMap);

        minMaxOfFinalMap.min += minMaxOfGround.min * groundFeaturesImpactOnLayout * 2;
        minMaxOfFinalMap.min += minMaxOfMountain.min * mountainFeaturesImpactOnLayout;

        minMaxOfFinalMap.max += minMaxOfGround.max * groundFeaturesImpactOnLayout * 2;
        minMaxOfFinalMap.max += minMaxOfMountain.max * mountainFeaturesImpactOnLayout;

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

        map.InitMap(noiseSubset.terrainMap, MeshGenerator.GenerateTerrainMesh(noiseSubset.finalNoiseMap, true));
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

    // A raw noise height (e.g. a region's noiseBase) in the normalized space of finalNoiseMap and the mesh
    public float NormalizedHeight(float rawHeight) {
        return Mathf.InverseLerp(minMaxOfFinalMap.min, minMaxOfFinalMap.max, rawHeight);
    }

    public float[,] GetFinalNoiseMap() {
        return finalNoiseMap;
    }

    /*
     * Tile-aligned sample layout (#35)
     *
     * Each tile's f samples span the tile edge to edge: sample k of a tile sits at k * f / (f - 1) sample-widths from the
     * tile's start, so the first and last samples are ON the tile boundaries. Neighbouring tiles' edge samples share a
     * position, so every slope lies inside one tile (previously samples sat at centres and the joining quad straddled
     * the boundary). Logic positions (MapCoordinate x/y) stay uniform: tile t spans [t*f, (t+1)*f).
     */

    // Floor division that works for negative samples (the overhang at -1 is the previous tile's last sample)
    private static int FloorDiv(int value, int divisor) {
        return value >= 0 ? value / divisor : -((-value + divisor - 1) / divisor);
    }

    // Position of a sample along one axis, in sample-widths from the map's origin
    public static float SamplePosition(int sample, int samplesPerTile) {
        int tile = FloorDiv(sample, samplesPerTile);
        int inTile = sample - tile * samplesPerTile;
        return tile * samplesPerTile + inTile * samplesPerTile / (float)(samplesPerTile - 1);
    }

    // Continuous sample index (tile * f + fractional in-tile index) for a position; inverse of SamplePosition within a tile
    private float SampleAtPosition(float position, int samplesPerTile, int totalSamples) {
        int tiles = totalSamples / samplesPerTile;
        int tile = Mathf.Clamp(Mathf.FloorToInt(position / samplesPerTile), 0, tiles - 1);
        float inTile = Mathf.Clamp01((position - tile * samplesPerTile) / samplesPerTile) * (samplesPerTile - 1);
        return tile * samplesPerTile + inTile;
    }

    // Height of the terrain surface at a MapCoordinate position (continuous), matching the mesh (#35)
    public float GetHeightAt(MapCoordinate mapCoordinate) {
        int f = Script.Get<Constants>().featuresPerLayoutPerAxis;
        int totalX = finalNoiseMap.GetLength(0);
        int totalY = finalNoiseMap.GetLength(1);

        float sampleX = SampleAtPosition(mapCoordinate.mapContainer.mapX * mapFullWidth + mapCoordinate.x, f, totalX);
        float sampleY = SampleAtPosition(mapCoordinate.mapContainer.mapY * mapFullHeight + mapCoordinate.y, f, totalY);

        // Bilinear between the 4 samples around the point, staying inside the tile
        int x0 = Mathf.FloorToInt(sampleX);
        int y0 = Mathf.FloorToInt(sampleY);
        int x1 = Mathf.Min(x0 + 1, (x0 / f) * f + f - 1);
        int y1 = Mathf.Min(y0 + 1, (y0 / f) * f + f - 1);
        float tx = sampleX - x0;
        float ty = sampleY - y0;

        float top = Mathf.Lerp(finalNoiseMap[x0, y0], finalNoiseMap[x1, y0], tx);
        float bottom = Mathf.Lerp(finalNoiseMap[x0, y1], finalNoiseMap[x1, y1], tx);
        return Mathf.Lerp(top, bottom, ty);
    }

    // Height of a sample by global index
    public float HeightAtSample(int globalX, int globalY) {
        return finalNoiseMap[globalX, globalY];
    }

    private KeyValuePair<MapContainer, float[,]> cachedMapContainer = new KeyValuePair<MapContainer, float[,]>();

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

                    int x = startXMapCoordinate + mapCoordinate.xLowSample;
                    int y = startYMapCoordinate + mapCoordinate.yLowSample;

                    // These coordinates are sample indices, so read the sample directly (GetHeightAt takes positions, #35)
                    terraformTarget.initialHeight[width, height] = finalNoiseMap[x, y];

                    float finalValue = MapAtXYWithFeatures(x, y, terrainMap, terraformTarget.terrainTypeTarget, heightAtNewRegion);

                    terraformTarget.heightTarget[width, height] = Mathf.InverseLerp(minMaxOfFinalMap.min, minMaxOfFinalMap.max, finalValue);
                }
            }

            CalculateNeighbourEdgeTargets(terraformTarget, startXLayout + layoutCoordinate.x, startYLayout + layoutCoordinate.y);
        }

        // Neighbouring tiles' edges follow the same animation (#29)
        for(int i = 0; i < terraformTarget.neighbourSamples.Count; i++) {
            Vector2Int sample = terraformTarget.neighbourSamples[i];
            finalNoiseMap[sample.x, sample.y] = Mathf.Lerp(terraformTarget.neighbourInitialHeight[i], terraformTarget.neighbourHeightTarget[i], terraformTarget.percentage);
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
        }

        float[,] finalSubsetNoiseMap = GetNoiseSubsetForMap(mapContainer).finalNoiseMap;
        ResetResourcePool();

        ResetCachedMap();

        return finalSubsetNoiseMap;
    }

    // Tile edges are shaped by their neighbours (see MapAtXYWithFeatures: a higher-region tile ramps down toward a lower
    // neighbour). When a tile changes terrain, its 4 neighbours' facing edges must be re-shaped too, or e.g. mining out a
    // mountain leaves the mountains beside it as sheer walls. Records the edge-band samples whose height changes once
    // the target tile has its new terrain type (#29). Global layout coordinates.
    const int edgeBand = 3;   // samples from a boundary that edge shaping can change (MapAtXYWithFeatures)

    // Width of an edge slope, in tile units (#35). 0.3 covers samples 0-2 of 10 (positions 0, 1/9, 2/9); keep it <= edgeBand / (f - 1).
    const float edgeRampWidth = 0.3f;

    private void CalculateNeighbourEdgeTargets(TerraformTarget terraformTarget, int tileX, int tileY) {
        int f = Script.Get<Constants>().featuresPerLayoutPerAxis;

        terraformTarget.neighbourSamples = new List<Vector2Int>();
        terraformTarget.neighbourInitialHeight = new List<float>();
        terraformTarget.neighbourHeightTarget = new List<float>();
        terraformTarget.neighbourTiles = new List<Vector2Int>();

        // Evaluate the neighbours as if the terraform were complete
        TerrainType savedTerrain = terrainMap[tileX, tileY];
        terrainMap[tileX, tileY] = terraformTarget.terrainTypeTarget;

        // All 8 neighbours: edge shaping also looks at diagonal tiles, which shapes corners (#35)
        for(int directionY = -1; directionY <= 1; directionY++) for(int directionX = -1; directionX <= 1; directionX++) {
            if(directionX == 0 && directionY == 0) {
                continue;
            }

            int neighbourX = tileX + directionX;
            int neighbourY = tileY + directionY;

            if(neighbourX < 0 || neighbourY < 0 || neighbourX >= terrainMap.GetLength(0) || neighbourY >= terrainMap.GetLength(1)) {
                continue;
            }

            bool changedTile = false;

            for(int localX = 0; localX < f; localX++) {
                for(int localY = 0; localY < f; localY++) {
                    int x = neighbourX * f + localX;
                    int y = neighbourY * f + localY;

                    // Only samples within edgeBand of the terraformed tile can change
                    int samplesAwayX = x < tileX * f ? tileX * f - x : (x >= (tileX + 1) * f ? x - (tileX + 1) * f + 1 : 0);
                    int samplesAwayY = y < tileY * f ? tileY * f - y : (y >= (tileY + 1) * f ? y - (tileY + 1) * f + 1 : 0);
                    if(samplesAwayX > edgeBand || samplesAwayY > edgeBand) {
                        continue;
                    }

                    float current = finalNoiseMap[x, y];
                    float target = Mathf.InverseLerp(minMaxOfFinalMap.min, minMaxOfFinalMap.max, MapAtXYWithFeatures(x, y, terrainMap));

                    if(Mathf.Abs(target - current) > 0.0001f) {
                        terraformTarget.neighbourSamples.Add(new Vector2Int(x, y));
                        terraformTarget.neighbourInitialHeight.Add(current);
                        terraformTarget.neighbourHeightTarget.Add(target);
                        changedTile = true;
                    }
                }
            }

            if(changedTile) {
                terraformTarget.neighbourTiles.Add(new Vector2Int(neighbourX, neighbourY));
            }
        }

        terrainMap[tileX, tileY] = savedTerrain;
    }

    // This container's slice of the final height map. Pooled: use it before calling again.
    public float[,] FinalHeightSubsetFor(MapContainer mapContainer) {
        float[,] subset = GetNoiseSubsetForMap(mapContainer).finalNoiseMap;
        ResetResourcePool();
        return subset;
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


        int sampleX = x / featuresPerLayoutPerAxis;
        int sampleY = y / featuresPerLayoutPerAxis;

        // Unknown (unexplored) tiles are shaped as the terrain they will be (#28/#29)
        TerrainType thisTerrainType = TerrainAtLocation(sampleX, sampleY);

        if (withAlternateTerrain != null) {
            thisTerrainType = withAlternateTerrain.Value;
        }

        float mapAtXY = SampleAtXY(x, y, thisTerrainType, withAlternateLayoutValue);

        // Edge shaping (#35). Where this tile borders a lower region (mountain beside ground, ground beside water) or the
        // world border, it eases down to that region's plateau, reaching it exactly on the shared boundary. With
        // tile-aligned samples the boundary samples of both tiles sit at the same position, so the surface meets with no
        // step and the whole slope belongs to the higher tile. Lower tiles stay flat (water level stays straight).
        // All 8 neighbours count (Euclidean distance), so corners close cleanly. Stays within edgeBand samples.
        TerrainManager terrainManager = Script.Get<TerrainManager>();
        int f = featuresPerLayoutPerAxis;
        float positionX = (x % f) / (float)(f - 1);   // this sample's position in its tile, 0..1 (tile units)
        float positionY = (y % f) / (float)(f - 1);
        int tilesX = usingTerrainMap.GetLength(0);
        int tilesY = usingTerrainMap.GetLength(1);
        float ownBase = terrainManager.regionTypeMap[thisTerrainType.regionType].noiseBase;

        float shaped = mapAtXY;

        for(int dy = -1; dy <= 1; dy++) {
            for(int dx = -1; dx <= 1; dx++) {
                if(dx == 0 && dy == 0) {
                    continue;
                }

                int neighbourX = sampleX + dx;
                int neighbourY = sampleY + dy;
                float neighbourLevel;

                if(neighbourX < 0 || neighbourY < 0 || neighbourX >= tilesX || neighbourY >= tilesY) {
                    if(dx != 0 && dy != 0) {
                        continue;
                    }

                    neighbourLevel = ownBase;   // world border
                } else {
                    TerrainType neighbour = TerrainAtLocation(neighbourX, neighbourY);
                    if(neighbour.priority >= thisTerrainType.priority) {
                        continue;
                    }

                    neighbourLevel = HeightAtRegion(terrainManager.regionTypeMap[neighbour.regionType]);
                }

                // Distance (tile units) from this sample to the neighbour's square
                float nearestX = Mathf.Clamp(positionX, dx, dx + 1);
                float nearestY = Mathf.Clamp(positionY, dy, dy + 1);
                float distance = Mathf.Sqrt((positionX - nearestX) * (positionX - nearestX) + (positionY - nearestY) * (positionY - nearestY));

                float t = Mathf.Clamp01(distance / edgeRampWidth);
                t = t * t * (3f - 2f * t);

                shaped = Mathf.Min(shaped, Mathf.Lerp(neighbourLevel, mapAtXY, t));
            }
        }

        return shaped;

    }

    // Sampling at an index for CreateMapWithFeatures
    // Feature noise at a sample, read at the sample's tile-aligned position (#35). Neighbouring tiles' edge samples share a
    // position, so tiles of the same region get identical heights on their shared edge (no step between two mountains).
    private float FeatureAtSample(float[,] featureMap, int x, int y) {
        int f = Script.Get<Constants>().featuresPerLayoutPerAxis;
        int width = featureMap.GetLength(0);
        int height = featureMap.GetLength(1);

        float positionX = Mathf.Clamp(SamplePosition(x, f), 0, width - 1);
        float positionY = Mathf.Clamp(SamplePosition(y, f), 0, height - 1);

        int x0 = Mathf.FloorToInt(positionX);
        int y0 = Mathf.FloorToInt(positionY);
        int x1 = Mathf.Min(x0 + 1, width - 1);
        int y1 = Mathf.Min(y0 + 1, height - 1);
        float tx = positionX - x0;
        float ty = positionY - y0;

        return Mathf.Lerp(Mathf.Lerp(featureMap[x0, y0], featureMap[x1, y0], tx), Mathf.Lerp(featureMap[x0, y1], featureMap[x1, y1], tx), ty);
    }

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
                return (sampleValue) + ((FeatureAtSample(groundFeaturesNoiseMap, x, y) * groundFeaturesImpactOnLayout * 2) - (groundFeaturesImpactOnLayout));
            case RegionType.Type.Mountain:
                return sampleValue + (FeatureAtSample(mountainFeaturesNoiseMap, x, y) * mountainFeaturesImpactOnLayout);
        }

        return 0;
    }

    /*
     * Creating color maps from finished noise maps 
     * */

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
