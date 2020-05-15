using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapContainerNeighbours {
    public MapContainer topMap, bottomMap, leftMap, rightMap;
    public MapContainer topLeftMap, topRightMap, bottomLeftMap, bottomRightMap;
}

public class MapContainer : MonoBehaviour, SelectionManagerDelegate, StatusEffectUpdateDelegate, IntervalActionDelegate {
    public Map map;

    public int mapX, mapY; // Virtual position within the maps manager
    public Rect mapRect; // World position within the Maps Manager

    public MapContainerNeighbours neighbours = new MapContainerNeighbours();

    MeshFilter cachedMeshFilter;
    MeshFilter meshFilter {
        get {
            if(cachedMeshFilter == null) {
                cachedMeshFilter = GetComponent<MeshFilter>();
            }

            return cachedMeshFilter;
        }
    }

    MeshRenderer cachedMeshRenderer;
    public MeshRenderer meshRenderer {
        get {
            if (cachedMeshRenderer == null) {
                cachedMeshRenderer = GetComponent<MeshRenderer>();
            }

            return cachedMeshRenderer;
        }
    }

    float[] textureIndexList;

    // Colliders
    const float colliderHighMultiplier = 2f;
    const int numCollidersPerTilePerAxis = 3;
    List<int> colliderIndexList = new List<int> { 2, 5, 9 };

    public bool isBuildingBoxColliders = false;
    BoxCollider[,][,] boxColliderArray;

    LayerMask boxColliderLayer;

    // Fog of War
    GameObject fogOfWarPrefab;
    FogOfWar[,] fogOfWarMap;
    public static float fogOfWarFadeInSeconds = 0.25f;
    public static float fogOfWarFadeOutDuration = 1.5f;

    const float extraFeaturePointOffset = 5f;
    Color fogStarterMaterialColor;

    // Behaviours
    Constants constants;
    MapsManager mapsManager;
    MapGenerator mapGenerator;

    SelectionManager selectionManager;
    BuildingManager buildingManager;
    GameResourceManager gameResourceManager;
    PlayerBehaviour playerBehaviour;

    // Constants
    int featuresWidthPerMap;
    int featuresHeightPerMap;

    private void Awake() {
        constants = Script.Get<Constants>();
        mapsManager = Script.Get<MapsManager>();
        mapGenerator = Script.Get<MapGenerator>();

        selectionManager = Script.Get<SelectionManager>();
        buildingManager = Script.Get<BuildingManager>();
        gameResourceManager = Script.Get<GameResourceManager>();
        playerBehaviour = Script.Get<PlayerBehaviour>();

        selectionManager.RegisterForNotifications(this);
        boxColliderLayer = LayerMask.NameToLayer("TerrainBoxCollider");

        fogOfWarPrefab = mapsManager.fogOfWarPrefab;
        fogOfWarMap = new FogOfWar[constants.layoutMapWidth, constants.layoutMapHeight];

        fogStarterMaterialColor = Color.black;
        fogStarterMaterialColor.a = 0f;

        featuresWidthPerMap = constants.layoutMapWidth * constants.featuresPerLayoutPerAxis;
        featuresHeightPerMap = constants.layoutMapHeight * constants.featuresPerLayoutPerAxis;

        int mapWidthWithOverhang = constants.layoutMapWidth + 2;
        int mapHeightWithOverhang = constants.layoutMapHeight + 2;

        textureIndexList = new float[mapWidthWithOverhang * mapHeightWithOverhang];

        gameObject.isStatic = true;
    }

    private void OnDestroy() {
        try {
            selectionManager.EndNotifications(this);
            buildingManager.EndStatusEffectNotifications(this);
        } catch(NullReferenceException) {}
    }

    /*
     * Public 
     * */

    public override bool Equals(object obj) {
        var container = obj as MapContainer;
        return container != null &&
               base.Equals(obj) &&
               mapX == container.mapX &&
               mapY == container.mapY;
    }

    public override int GetHashCode() {
        var hashCode = -820503359;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + mapX.GetHashCode();
        hashCode = hashCode * -1521134295 + mapY.GetHashCode();
        return hashCode;
    }

    public void SetMapPosition(int mapX, int mapY, Rect mapRect) {
        this.mapX = mapX;
        this.mapY = mapY;
        this.mapRect = mapRect;

        buildingManager.RegisterForStatusEffectNotifications(this);
    }

    public void setMap(Map map, bool withColliders = true) {

        if (this.map != null) {
            RemoveBoxColliders();
        }        

        this.map = map;
        map.mapContainer = this;

        map.CreateAllActionableItemOverrides();
        gameResourceManager.RegisterMapForMinerals(map);

        DrawMesh();

        if (withColliders == true) {
            Constants constants = Script.Get<Constants>();

            int width = map.mapWidth / map.featuresPerLayoutPerAxis;
            int height = map.mapHeight / map.featuresPerLayoutPerAxis;

            boxColliderArray = new BoxCollider[width, height][,];

            // We will create box colliders as fog of war disappears
            //StartCoroutine(AddBoxColliders());
        }

        //SetupFogOfWar();
        SetupMaterialShader();

        map.transform.SetParent(this.transform, true);
    }

    public void DrawMesh() {
        meshFilter.sharedMesh = map.meshData.FinalizeMesh();
        meshRenderer.sharedMaterial.mainTexture = map.meshTexture;
    }

    public void UpdateMapOverhang() {
        MeshGenerator.UpdateMeshOverhang(map.meshData, neighbours);

        UpdateMaterialOverhangTextures();
        DrawMesh();
    }

    /*
    * IntervalActionDelegate Interface
    * */

    bool canCreateBox;
    public void PerformIntervalAction() {
        canCreateBox = true;
    }

    /*
     * Box Colliders
     * */

    private IEnumerator CreateBoxCollidersAtCoordinate(LayoutCoordinate layoutCoordinate, Action ended) {
        Constants constants = Script.Get<Constants>();

        if(boxColliderArray == null) {
            yield break;
        }

        int width = map.mapWidth / map.featuresPerLayoutPerAxis;
        int height = map.mapHeight / map.featuresPerLayoutPerAxis;

        float layoutSquareSizeX = map.featuresPerLayoutPerAxis;
        float layoutSquareSizeY = map.featuresPerLayoutPerAxis;

        float boxSizeX = layoutSquareSizeX / numCollidersPerTilePerAxis;
        float boxSizeY = layoutSquareSizeY / numCollidersPerTilePerAxis;

        float halfTotalWidth = layoutSquareSizeX * width / 2f;
        float halfTotalHeight = layoutSquareSizeY * height / 2f;

        int x = layoutCoordinate.x;
        int y = height - 1 - layoutCoordinate.y;

        float xPos = (x * layoutSquareSizeX - halfTotalWidth);
        float yPos = (y * layoutSquareSizeY - halfTotalHeight);

        // Sign up for IntervalActions
        IntervalActionPipeline pipeline = Script.Get<IntervalActionPipeline>();
        pipeline.Add(this);

        //PathGridCoordinate[][] gridCoordinates = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);
        MapCoordinate[,] mapCoordinates = MapCoordinate.MapCoordinatesFromLayoutCoordinate(layoutCoordinate);

        Dictionary<MapCoordinate, float> cachedHeights = CacheHightsAround(layoutCoordinate);

        for(int w = 0; w < numCollidersPerTilePerAxis; w++) {
            for(int h = 0; h < numCollidersPerTilePerAxis; h++) {
                //if (w != 0 && h != 0 && w != numColliders -1 && h != numColliders - 1) {
                //    continue;
                //}

                yield return new WaitUntil(delegate {
                    return canCreateBox;
                });

                canCreateBox = false;

                if(boxColliderArray[x, y] == null) {
                    boxColliderArray[x, y] = new BoxCollider[numCollidersPerTilePerAxis, numCollidersPerTilePerAxis];
                }

                if(boxColliderArray[x, y][w, h] != null) {
                    continue;
                }

                int sampleW = colliderIndexList[w];
                int sampleH = colliderIndexList[h];

                int realWorldH = constants.featuresPerLayoutPerAxis - 1 - sampleH;

                //MapCoordinate mapCoordinate = MapCoordinate.FromGridCoordinate(gridCoordinates[w][sampleH]);
                MapCoordinate mapCoordinate = mapCoordinates[sampleW, realWorldH];
                //MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);

                BoxCollider boxCollider = gameObject.AddComponent(typeof(BoxCollider)) as BoxCollider;

                boxCollider.gameObject.layer = boxColliderLayer;

                float wPos = (w * boxSizeX) + boxSizeX / 2f;
                float hPos = (h * boxSizeY) + boxSizeY / 2f;

                boxCollider.center = new Vector3(xPos + wPos, 0, yPos + hPos);
                boxCollider.size = new Vector3(boxSizeX, GetHeightAround(mapCoordinate, 5, cachedHeights) * colliderHighMultiplier, boxSizeY);

                boxColliderArray[x, y][w, h] = boxCollider;
            }
        }

        pipeline.Remove(this);
        ended();
    }

    private void RemoveBoxColliders() {
        if(boxColliderArray != null) {
            int width = boxColliderArray.GetLength(0);
            int height = boxColliderArray.GetLength(1);

            for(int x = 0; x < width; x++) {
                for(int y = 0; y < height; y++) {                    

                    foreach(BoxCollider collider in boxColliderArray[x, y]) {
                        DestroyImmediate(collider);
                    }
                }
            }
        } else {
            BoxCollider[] colliders = GetComponents<BoxCollider>();
            foreach(BoxCollider collider in colliders) {
                DestroyImmediate(collider);
            }
        }       
    }
    
    public void ResizeBoxColliderAt(LayoutCoordinate layoutCoordinate) {

        if (boxColliderArray == null) {
            return;
        }

        MapCoordinate[,] mapCoordinates = MapCoordinate.MapCoordinatesFromLayoutCoordinate(layoutCoordinate);
        Dictionary<MapCoordinate, float> cachedHeights = CacheHightsAround(layoutCoordinate);
    
        int x = layoutCoordinate.x;
        int y = (map.mapHeight / map.featuresPerLayoutPerAxis) - 1 - layoutCoordinate.y;

        if (boxColliderArray[x, y] == null) {
            return;
        }

        for(int w = 0; w < numCollidersPerTilePerAxis; w++) {
            for(int h = 0; h < numCollidersPerTilePerAxis; h++) {

                BoxCollider boxCollider = boxColliderArray[x, y][w, h];

                if (boxCollider == null) {
                    continue;
                }

                int sampleW = colliderIndexList[w];
                int sampleH = colliderIndexList[h];

                int realWorldH = constants.featuresPerLayoutPerAxis - 1 - sampleH;

                MapCoordinate mapCoordinate = mapCoordinates[sampleW, realWorldH];

                boxCollider.size = new Vector3(boxCollider.size.x, GetHeightAround(mapCoordinate, 5, cachedHeights) * colliderHighMultiplier, boxCollider.size.z);
                boxColliderArray[x, y][w, h] = boxCollider;
            }
        }
    }

    private Dictionary<MapCoordinate, float> CacheHightsAround(LayoutCoordinate layoutCoordinate) {
        Dictionary<MapCoordinate, float> cachedHeights = new Dictionary<MapCoordinate, float>();

        foreach(MapCoordinate mapCoordinate in MapCoordinate.MapCoordinatesFromLayoutCoordinate(layoutCoordinate)) {
            cachedHeights[mapCoordinate] = mapCoordinate.mapContainer.map.getHeightAt(mapCoordinate);
        }

        return cachedHeights;
    }

    private float GetHeightAround(MapCoordinate mapCoordinate, int radius, Dictionary<MapCoordinate, float> cachedHeights) {
        float heightTotal = 0;        

        for(int x = - radius; x <= radius; x++) {
            for(int y = -radius; y <= radius; y++) {
                heightTotal += cachedHeights[mapCoordinate];
            }
        }

        int diameter = radius * 2 + 1;

        return heightTotal / (diameter * diameter);
    }

    /*
     * Fog of War
     * */

    /*private void SetupFogOfWar() {       
        Constants constants = Script.Get<Constants>();

        int width = constants.layoutMapWidth;
        int height = constants.layoutMapHeight;

        fogOfWarMap = new FogOfWar[width, height];

        float boxSizeX = constants.featuresPerLayoutPerAxis;
        float boxSizeZ = constants.featuresPerLayoutPerAxis;

        float halfTotalWidth = boxSizeX * width / 2f;
        float halfTotalHeight = boxSizeZ * height / 2f;

        Color materialColor = Color.black;
        materialColor.a = 0.0f;

        MapsManager mapsManager = Script.Get<MapsManager>();
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        GameObject prefab = mapsManager.fogOfWarPrefab;
        //float[,] originalFinalNoiseMap = mapGenerator.GetOriginalFinalNosieMapForMap(this);

        //float[,] fullOriginalFinalNoiseMap = mapGenerator.GetFullOriginalNoiseMap();

        //int tileSize = constants.featuresPerLayoutPerAxis;

        const float featurePointOffset = 5f;

        int featuresWidthPerMap = constants.layoutMapWidth * constants.featuresPerLayoutPerAxis;
        int featuresHeightPerMap = constants.layoutMapHeight * constants.featuresPerLayoutPerAxis;

        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {

                if (fogOfWarMap[x, y] == null) {
                    GameObject newCube = Instantiate(prefab);
                    newCube.name = "Fog " + x + ", " + y;

                    fogOfWarMap[x, y] = newCube.GetComponent<FogOfWar>();
                }

                FogOfWar fog = fogOfWarMap[x, y];

                float xOffset = 0;
                float yOffset = 0;

                int startX = (mapX * featuresWidthPerMap) + (x * constants.featuresPerLayoutPerAxis);
                int tileWidth = constants.featuresPerLayoutPerAxis;

                int startY = (mapY * featuresHeightPerMap) + (y * constants.featuresPerLayoutPerAxis);
                int tileHeight = constants.featuresPerLayoutPerAxis;

                //if(startX != 0) {
                //    startX -= 1;
                //    tileWidth += 1;
                //    xOffset -= featurePointOffset;
                //}

                //if(startY != 0) {
                //    startY -= 1;
                //    tileHeight += 1;
                //    yOffset += featurePointOffset;
                //}

                //if(x != width -1 || mapX != constants.mapCountX - 1) {
                //    tileWidth += 1;
                //    xOffset += featurePointOffset;
                //}

                //if(y != height - 1 || mapY != constants.mapCountY - 1) {
                //    tileHeight += 1;
                //    yOffset -= featurePointOffset;
                //}

                float[,] tileNoiseMap = MapGenerator.RangeSubset(fullOriginalFinalNoiseMap, startX, startY, tileWidth, tileHeight);
                MeshData fogMesh = MeshGenerator.GenerateTerrainMesh(tileNoiseMap);

                fog.meshFilter.mesh = fogMesh.FinalizeMesh();
                fog.meshRenderer.material.color = materialColor;

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, this);
                MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
                WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                Vector3 cubePosition = worldPosition.vector3;
                 
                fog.transform.SetParent(transform);
                fog.transform.position = new Vector3(cubePosition.x + xOffset, 1f, cubePosition.z + yOffset);
                fog.transform.localScale = new Vector3(1, 0.33f, 1);
            }
        }
    }*/

    private IEnumerator CreateFogOfWarAt(int x, int y, Action completion) {

        // TODO: Seperate thread?
        yield return null;

        float[,] finalNoiseMap = mapGenerator.GetFinalNoiseMap();

        if(fogOfWarMap[x, y] == null) {
            GameObject newCube = Instantiate(fogOfWarPrefab);
            newCube.name = "Fog " + x + ", " + y;

            fogOfWarMap[x, y] = newCube.GetComponent<FogOfWar>();
        }

        FogOfWar fog = fogOfWarMap[x, y];

        float xOffset = 0;
        float yOffset = 0;

        int startX = (mapX * featuresWidthPerMap) + (x * constants.featuresPerLayoutPerAxis);
        int tileWidth = constants.featuresPerLayoutPerAxis;

        int startY = (mapY * featuresHeightPerMap) + (y * constants.featuresPerLayoutPerAxis);
        int tileHeight = constants.featuresPerLayoutPerAxis;

        if(startX != 0) {
            startX -= 1;
            tileWidth += 1;
            xOffset -= extraFeaturePointOffset;
        }

        if(startY != 0) {
            startY -= 1;
            tileHeight += 1;
            yOffset += extraFeaturePointOffset;
        }

        if(x != constants.layoutMapWidth - 1 || mapX != constants.mapCountX - 1) {
            tileWidth += 1;
            xOffset += extraFeaturePointOffset;
        }

        if(y != constants.layoutMapHeight - 1 || mapY != constants.mapCountY - 1) {
            tileHeight += 1;
            yOffset -= extraFeaturePointOffset;
        }

        float[,] tileNoiseMap = MapGenerator.RangeSubset(finalNoiseMap, startX, startY, tileWidth, tileHeight);
        MeshData fogMesh = MeshGenerator.GenerateTerrainMesh(tileNoiseMap);

        fog.meshFilter.mesh = fogMesh.FinalizeMesh();
        fog.meshRenderer.material.color = fogStarterMaterialColor;

        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, this);
        MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
        WorldPosition worldPosition = new WorldPosition(mapCoordinate);

        Vector3 cubePosition = worldPosition.vector3;

        fog.transform.SetParent(transform);
        fog.transform.position = new Vector3(cubePosition.x + xOffset, 1f, cubePosition.z + yOffset);
        fog.transform.localScale = Vector3.one;

        completion?.Invoke();
    }

    /*
     * Textures and Overhang Updates
     * */

    private void UpdateMaterialOverhangTextures() {

        Constants constants = Script.Get<Constants>();
        Material mapMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        TextureGenerator texGen = Script.Get<TextureGenerator>();

        int mapWidthWithOverhang = constants.layoutMapWidth + 2;
        int mapHeightWithOverhang = constants.layoutMapHeight + 2;

        // Left side

        // Left edge
        int x = 0;
        int y = 0;

        for(y = 0; y < constants.layoutMapHeight + 2; y++) {

            if(y == 0 && neighbours.topLeftMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(constants.layoutMapWidth - 1, constants.layoutMapHeight - 1, neighbours.topLeftMap);

                TerrainType terrain = neighbours.topLeftMap.map.GetTerrainAt(coordinate);
                textureIndexList[y * mapWidthWithOverhang] = texGen.RegionTypeTextureIndex(terrain);
            }

            if(y > 0 && y < constants.layoutMapHeight + 1 && neighbours.leftMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(constants.layoutMapWidth - 1, (y - 1), neighbours.leftMap);

                TerrainType terrain = neighbours.leftMap.map.GetTerrainAt(coordinate);
                textureIndexList[y * mapWidthWithOverhang] = texGen.RegionTypeTextureIndex(terrain);
            }

            if(y == constants.layoutMapHeight + 1 && neighbours.bottomLeftMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(constants.layoutMapWidth - 1, 0, neighbours.bottomLeftMap);

                TerrainType terrain = neighbours.bottomLeftMap.map.GetTerrainAt(coordinate);
                textureIndexList[y * mapWidthWithOverhang] = texGen.RegionTypeTextureIndex(terrain);
            }
        }

        // Right edge
        x = mapWidthWithOverhang - 1;

        for(y = 0; y < constants.layoutMapHeight + 2; y++) {
            if(y == 0 && neighbours.topRightMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(0, constants.layoutMapHeight - 1, neighbours.topRightMap);

                TerrainType terrain = neighbours.topRightMap.map.GetTerrainAt(coordinate);
                textureIndexList[y + mapWidthWithOverhang + x] = texGen.RegionTypeTextureIndex(terrain);
            }

            if(y > 0 && y < constants.layoutMapHeight + 1 && neighbours.rightMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(0, y - 1, neighbours.rightMap);

                TerrainType terrain = neighbours.rightMap.map.GetTerrainAt(coordinate);
                textureIndexList[y * mapWidthWithOverhang + x] = texGen.RegionTypeTextureIndex(terrain);
            }

            if(y == constants.layoutMapHeight + 1 && neighbours.bottomRightMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(0, 0, neighbours.bottomRightMap);

                if(neighbours.bottomRightMap.map == null) {
                }

                TerrainType terrain = neighbours.bottomRightMap.map.GetTerrainAt(coordinate);
                textureIndexList[y * mapWidthWithOverhang + x] = texGen.RegionTypeTextureIndex(terrain);
            }

        }

        // Top 
        y = 0;

        for(x = 1; x < constants.layoutMapWidth + 1; x++) {
            if(neighbours.topMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(x - 1, constants.layoutMapHeight - 1, neighbours.topMap);

                TerrainType terrain = neighbours.topMap.map.GetTerrainAt(coordinate);
                textureIndexList[y * mapWidthWithOverhang + x] = texGen.RegionTypeTextureIndex(terrain);
            }
        }
        
        //Bottom
        y = mapHeightWithOverhang - 1;

        for(x = 1; x < mapWidthWithOverhang - 1; x++) {
            if(neighbours.bottomMap != null) {
                LayoutCoordinate coordinate = new LayoutCoordinate(x - 1, 0, neighbours.bottomMap);

                TerrainType terrain = neighbours.bottomMap.map.GetTerrainAt(coordinate);
                textureIndexList[y * mapWidthWithOverhang + x] = texGen.RegionTypeTextureIndex(terrain);
            }
        }


        mapMaterial.SetFloatArray("layoutTextures", textureIndexList);    
    }

    public void SetupMaterialShader() {
        Constants constants = Script.Get<Constants>();
        Material mapMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        mapMaterial.SetFloat("tileSize", constants.featuresPerLayoutPerAxis);

        int mapWidthWithOverhang = constants.layoutMapWidth + 2;
        int mapHeightWithOverhang = constants.layoutMapHeight + 2;

        mapMaterial.SetFloat("mapLayoutWidth", mapWidthWithOverhang);
        mapMaterial.SetFloat("mapLayoutHeight", mapHeightWithOverhang);


        mapMaterial.SetVector("mapLayout", new Vector2(mapWidthWithOverhang, mapHeightWithOverhang));

        //mapMaterial.SetFloat("mapXOffsetLow", 0 - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
        //mapMaterial.SetFloat("mapXOffsetHigh", constants.layoutMapWidth - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

        //mapMaterial.SetFloat("mapYOffsetLow", 0 - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
        //mapMaterial.SetFloat("mapYOffsetHigh", constants.layoutMapHeight - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));

        float mapXOffsetLow = 0 - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f);
        float mapXOffsetHigh = constants.layoutMapWidth - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f);

        float mapYOffsetLow = 0 - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f);
        float mapYOffsetHigh = constants.layoutMapHeight - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f);

        mapMaterial.SetVector("mapOffset", new Vector4(mapXOffsetLow, mapXOffsetHigh, mapYOffsetLow, mapYOffsetHigh));

        TextureGenerator texGen = Script.Get<TextureGenerator>();

        UpdateShaderTerrainTextures();

        mapMaterial.SetFloatArray("indexPriority", texGen.TexturePriorityList());
        mapMaterial.SetFloatArray("indexScale", texGen.TextureScaleList());

        Texture2DArray texturesArray = texGen.TextureArray();
        mapMaterial.SetTexture("baseTextures", texturesArray);

        Texture2DArray bumpArray = texGen.BumpMapArray();
        mapMaterial.SetTexture("bumpMapTextures", bumpArray);
    }

    public void UpdateShaderTerrainTextures() {
        Constants constants = Script.Get<Constants>();
        Material mapMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        int mapWidthWithOverhang = constants.layoutMapWidth + 2;
        int mapHeightWithOverhang = constants.layoutMapHeight + 2;

        TextureGenerator texGen = Script.Get<TextureGenerator>();

        for(int y = 0; y < mapHeightWithOverhang; y++) {
            for(int x = 0; x < mapWidthWithOverhang; x++) {

                if(x == 0 || x == mapWidthWithOverhang - 1 || y == 0 || y == mapHeightWithOverhang - 1) {
                    //textureIndexList[y * mapWidthWithOverhang + x] = -1;
                    continue;
                }

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x - 1, y - 1, this);
                TerrainType terrain = map.GetTerrainAt(layoutCoordinate);

                textureIndexList[y * mapWidthWithOverhang + x] = texGen.RegionTypeTextureIndex(terrain);
            }
        }

        mapMaterial.SetFloatArray("layoutTextures", textureIndexList);
    }

    public void UpdateNeibourOverhangsForCoordinate(LayoutCoordinate coordinate) {
        int layoutWidth = constants.layoutMapWidth;
        int layoutHeight = constants.layoutMapHeight;

        // Update neibour overhang
        if(coordinate.x == layoutWidth - 1) {

            if(coordinate.y == 0) {
                if(neighbours.topRightMap != null) {
                    neighbours.topRightMap.UpdateMapOverhang();
                }
            } else if(coordinate.y == layoutHeight - 1) {
                if(neighbours.bottomRightMap != null) {
                    neighbours.bottomRightMap.UpdateMapOverhang();
                }
            }

            if(neighbours.rightMap != null) {
                neighbours.rightMap.UpdateMapOverhang();
            }
        }

        if(coordinate.y == layoutHeight - 1 && neighbours.bottomMap != null) {
            neighbours.bottomMap.UpdateMapOverhang();
        }
    }

    /*
     * SelectionManagerDelegate Interface
     * */

    public void NotifyUpdateSelection(Selection selection) {
        Material mapMaterial = meshRenderer.material;

        if(selection != null && selection.selectionType == Selection.SelectionType.Terrain && selection.coordinate.mapContainer == this) {
            mapMaterial.SetFloat("hasSelection", 1);
            mapMaterial.SetVector("selection", new Vector2(selection.coordinate.x, selection.coordinate.y));
        } else {
            mapMaterial.SetFloat("hasSelection", 0);
            mapMaterial.SetVector("selection", new Vector2(-1, -1));
        }
    }

    /*
     * StatusEffectUpdateDelegate Interface
     * */

    public void StatusEffectMapUpdated(BuildingEffectStatus[,] statusMap, List<KeyValuePair<int, int>> effectedIndicies) {

        int width = constants.layoutMapWidth;
        int height = constants.layoutMapHeight;

        List<KeyValuePair<LayoutCoordinate, TerrainType>> fogInfoList = new List<KeyValuePair<LayoutCoordinate, TerrainType>>();

        foreach(KeyValuePair<int, int> index in effectedIndicies) {
            int sampleX = index.Key;
            int sampleY = index.Value;

            int x = sampleX - (mapX * width);
            int y = sampleY - (mapY * height);

            if(x < 0 || x >= width || y < 0 || y >= height) {
                continue;
            }

            if((statusMap[sampleX, sampleY] & BuildingEffectStatus.Light) == BuildingEffectStatus.Light) {

                // TODO: Test if we need this?
                if(gameObject.activeInHierarchy == false) { gameObject.SetActive(true); }

                // Terraform to known terrain type
                LayoutCoordinate coordinate = new LayoutCoordinate(x, y, this);
                TerrainType terrainType = mapGenerator.KnownTerrainTypeAtIndex(sampleX, sampleY);

                TerraformTarget terraformTarget = new TerraformTarget(coordinate, terrainType);
                terraformTarget.percentage = 1.0f;

                // Do terraform to original terrain type DATA ONLY as indicated by last param
                // Once the fog has been created and faded in, perform actual mesh and texture terrain updates hidden under fo
                map.TerraformHeightMap(terraformTarget, true);

                fogInfoList.Add(new KeyValuePair<LayoutCoordinate, TerrainType>(coordinate, terrainType));
            }
        }

        TimeManager timeManager = Script.Get<TimeManager>();
        GameObject prefab = mapsManager.fogOfWarPrefab;

        float[,] finalNoiseMap = mapGenerator.GetFinalNoiseMap();

        int totalCouroutinesStarted = 0;
        Action createBoxEnded = () => {
            totalCouroutinesStarted--;

            if(totalCouroutinesStarted <= 0) {
                isBuildingBoxColliders = false;

                // Kick off all fog animations
                StartCoroutine(AnimateIntoFog(fogInfoList, () => {

                    // Update our terrain mesh and textures as a second step from first loop                    
                    DrawMesh();
                    foreach(KeyValuePair<LayoutCoordinate, TerrainType> coordinate in fogInfoList) {
                        map.UpdateTerrainAtLocation(coordinate.Key, coordinate.Value);
                        UpdateNeibourOverhangsForCoordinate(coordinate.Key);
                    }

                    StartCoroutine(DisolveFog(fogInfoList));
                }));
            }
        };

        foreach(KeyValuePair<int, int> index in effectedIndicies) {
            int sampleX = index.Key;
            int sampleY = index.Value;

            int x = sampleX - (mapX * width);
            int y = sampleY - (mapY * height);

            if(x < 0 || x >= width || y < 0 || y >= height) {
                continue;
            }

            if((statusMap[sampleX, sampleY] & BuildingEffectStatus.Light) == BuildingEffectStatus.Light) {

                int holdX = x;
                int holdY = y;

                // Create the fog
                isBuildingBoxColliders = true;
                totalCouroutinesStarted++;

                StartCoroutine(CreateFogOfWarAt(x, y, () => {                    
                    StartCoroutine(CreateBoxCollidersAtCoordinate(new LayoutCoordinate(x, y, this), createBoxEnded));
                }));
            }
        }
    }


    private IEnumerator AnimateIntoFog(List<KeyValuePair<LayoutCoordinate, TerrainType>> infoList, Action competion) {

        // Fade in
        float fadePercent = 0;

        while(fadePercent < 1) {
            yield return null;

            if(playerBehaviour.gamePaused) {
                continue;
            }

            foreach(KeyValuePair<LayoutCoordinate, TerrainType> coordinate in infoList) {
                FogOfWar fog = fogOfWarMap[coordinate.Key.x, coordinate.Key.y];

                Color color = fog.meshRenderer.material.color;
                color.a = fadePercent;

                fog.meshRenderer.material.color = color;
            }

            fadePercent += Time.deltaTime * (1.0f / fogOfWarFadeInSeconds);
        }

        // Switch to disolve material
        foreach(KeyValuePair<LayoutCoordinate, TerrainType> info in infoList) {
            Material material = fogOfWarMap[info.Key.x, info.Key.y].disolveMaterial;
            fogOfWarMap[info.Key.x, info.Key.y].meshRenderer.material = material;
        }

        yield return null;      

        competion?.Invoke();
    }

    private IEnumerator DisolveFog(List<KeyValuePair<LayoutCoordinate, TerrainType>> coordinateList) {

        // Disolve
        float fadePercent = 0;

        while(fadePercent < 1) {
            yield return null;

            if(playerBehaviour.gamePaused) {
                continue;
            }

            foreach(KeyValuePair<LayoutCoordinate, TerrainType> coordinate in coordinateList) {
                fogOfWarMap[coordinate.Key.x, coordinate.Key.y].meshRenderer.material.SetFloat("_Cutoff", fadePercent);
            }

            fadePercent += Time.deltaTime * (1.0f / fogOfWarFadeOutDuration);
        }

        // Remove Objects
        foreach(KeyValuePair<LayoutCoordinate, TerrainType> coordinate in coordinateList) {
            Destroy(fogOfWarMap[coordinate.Key.x, coordinate.Key.y]);
            fogOfWarMap[coordinate.Key.x, coordinate.Key.y] = null;

            yield return null;
        }
    }
}
