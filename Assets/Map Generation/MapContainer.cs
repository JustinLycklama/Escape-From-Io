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

    public bool isBuildingBoxColliders = false;
    BoxCollider[,][,] boxColliderArray;

    FogOfWar[,] fogOfWarMap;
    public static int fogOfWarFadeInDuration = 1;
    public static int fogOfWarFadeOutDuration = 1;

    float[] textureIndexList;

    public MapContainerNeighbours neighbours = new MapContainerNeighbours();

    private LayerMask terrainLayer;

    private void Awake() {
        Constants constants = Script.Get<Constants>();

        int mapWidthWithOverhang = constants.layoutMapWidth + 2;
        int mapHeightWithOverhang = constants.layoutMapHeight + 2;

        textureIndexList = new float[mapWidthWithOverhang * mapHeightWithOverhang];

        gameObject.isStatic = true;
        terrainLayer = LayerMask.NameToLayer("TerrainBoxCollider");
    }

    private void Start() {
        Script.Get<SelectionManager>().RegisterForNotifications(this);
    }

    private void OnDestroy() {
        try {
            Script.Get<SelectionManager>().EndNotifications(this);
            Script.Get<BuildingManager>().EndStatusEffectNotifications(this);
        } catch(NullReferenceException) {}
    }

    public void SetMapPosition(int mapX, int mapY, Rect mapRect) {
        this.mapX = mapX;
        this.mapY = mapY;
        this.mapRect = mapRect;

        Script.Get<BuildingManager>().RegisterForStatusEffectNotifications(this);
    }

    public void setMap(Map map, bool withColliders = true) {

        if (this.map != null) {
            RemoveBoxColliders();
        }        

        this.map = map;
        map.mapContainer = this;

        map.CreateAllActionableItemOverrides();
        Script.Get<GameResourceManager>().RegisterMapForMinerals(map);

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
     * Box Colliders
     * */

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

    const int numColliders = 3; // constants.nodesPerLayoutPerAxis; // featuresPerLayoutPerAxis
    List<int> indexList = new List<int> { 2, 5, 9 };

    //private IEnumerator AddBoxColliders() {

    //    Constants constants = Script.Get<Constants>();

    //    int width = map.mapWidth / map.featuresPerLayoutPerAxis;
    //    int height = map.mapHeight / map.featuresPerLayoutPerAxis;

    //    boxColliderArray = new BoxCollider[width, height][,];

    //    for(int x = 0; x < width; x++) {
    //        for(int y = 0; y < height; y++) {



    //            boxColliderArray[x, y] = new BoxCollider[numColliders, numColliders];

    //            LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, height - 1 - y, this);

    //        }
    //    }

    //    isBuildingBoxColliders = false;
    //}    

    // IntervalActionDelegate
    bool canCreateBox;
    public void PerformIntervalAction() {
        canCreateBox = true;
    }

    private IEnumerator CreateBoxCollidersAtCoordinate(LayoutCoordinate layoutCoordinate, Action ended) {
        Constants constants = Script.Get<Constants>();

        if (boxColliderArray == null) {
            yield break;
        }

        int width = map.mapWidth / map.featuresPerLayoutPerAxis;
        int height = map.mapHeight / map.featuresPerLayoutPerAxis;

        float layoutSquareSizeX = map.featuresPerLayoutPerAxis;
        float layoutSquareSizeY = map.featuresPerLayoutPerAxis;

        float boxSizeX = layoutSquareSizeX / numColliders;
        float boxSizeY = layoutSquareSizeY / numColliders;

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

        for(int w = 0; w < numColliders; w++) {
            for(int h = 0; h < numColliders; h++) {                
                //if (w != 0 && h != 0 && w != numColliders -1 && h != numColliders - 1) {
                //    continue;
                //}

                yield return new WaitUntil(delegate {
                    return canCreateBox;
                });

                canCreateBox = false;

                if (boxColliderArray[x, y] == null) {
                    boxColliderArray[x, y] = new BoxCollider[numColliders, numColliders];
                }

                if (boxColliderArray[x, y][w, h] != null) {
                    continue;
                }

                int sampleW = indexList[w];
                int sampleH = indexList[h];

                int realWorldH = constants.featuresPerLayoutPerAxis - 1 - sampleH;

                //MapCoordinate mapCoordinate = MapCoordinate.FromGridCoordinate(gridCoordinates[w][sampleH]);
                MapCoordinate mapCoordinate = mapCoordinates[sampleW, realWorldH];
                //MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);

                BoxCollider boxCollider = gameObject.AddComponent(typeof(BoxCollider)) as BoxCollider;

                boxCollider.gameObject.layer = terrainLayer;

                float wPos = (w * boxSizeX) + boxSizeX / 2f;
                float hPos = (h * boxSizeY) + boxSizeY / 2f;

                boxCollider.center = new Vector3(xPos + wPos, 0, yPos + hPos);
                boxCollider.size = new Vector3(boxSizeX, GetHeightAround(mapCoordinate, 5, cachedHeights) * highMultiplier, boxSizeY);

                boxColliderArray[x, y][w, h] = boxCollider;
            }
        }

        pipeline.Remove(this);
        ended();
    }

    const float highMultiplier = 2f;// * (9f / 10f);
    public void ResizeBoxColliderAt(LayoutCoordinate layoutCoordinate) {
        Constants constants = Script.Get<Constants>();

        if (boxColliderArray == null) {
            return;
        }

        //PathGridCoordinate[][] gridCoordinates = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);
        MapCoordinate[,] mapCoordinates = MapCoordinate.MapCoordinatesFromLayoutCoordinate(layoutCoordinate);
        Dictionary<MapCoordinate, float> cachedHeights = CacheHightsAround(layoutCoordinate);
    
        int x = layoutCoordinate.x;
        int y = (map.mapHeight / map.featuresPerLayoutPerAxis) - 1 - layoutCoordinate.y;

        if (boxColliderArray[x, y] == null) {
            return;
        }

        for(int w = 0; w < numColliders; w++) {
            for(int h = 0; h < numColliders; h++) {

                BoxCollider boxCollider = boxColliderArray[x, y][w, h];

                if (boxCollider == null) {
                    continue;
                }

                int sampleW = indexList[w];
                int sampleH = indexList[h];

                int realWorldH = constants.featuresPerLayoutPerAxis - 1 - sampleH;

                //MapCoordinate mapCoordinate = MapCoordinate.FromGridCoordinate(gridCoordinates[w][sampleH]);
                MapCoordinate mapCoordinate = mapCoordinates[sampleW, realWorldH];
                //MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);

                boxCollider.size = new Vector3(boxCollider.size.x, GetHeightAround(mapCoordinate, 5, cachedHeights) * highMultiplier, boxCollider.size.z);
                boxColliderArray[x, y][w, h] = boxCollider;
            }
        }
    }

    //private Dictionary<MapCoordinate, float> cachedHeights = new Dictionary<MapCoordinate, float>();
    private Dictionary<MapCoordinate, float> CacheHightsAround(LayoutCoordinate layoutCoordinate) {
        Dictionary<MapCoordinate, float> cachedHeights = new Dictionary<MapCoordinate, float>();

        foreach(MapCoordinate mapCoordinate in MapCoordinate.MapCoordinatesFromLayoutCoordinate(layoutCoordinate)) {
            cachedHeights[mapCoordinate] = mapCoordinate.mapContainer.map.getHeightAt(mapCoordinate);
        }

        return cachedHeights;
    }

    private float GetHeightAround(MapCoordinate mapCoordinate, int radius, Dictionary<MapCoordinate, float> cachedHeights) {
        Constants constants = Script.Get<Constants>();
        float heightTotal = 0;        

        for(int x = - radius; x <= radius; x++) {
            for(int y = -radius; y <= radius; y++) {
                //float localX = Mathf.Clamp(mapCoordinate.x + x, 0, constants.featuresPerLayoutPerAxis);
                //float localY = Mathf.Clamp(mapCoordinate.y + y, 0, constants.featuresPerLayoutPerAxis);

                //MapCoordinate localCoordinate = new MapCoordinate(localX, localY, mapCoordinate.mapContainer);

                //heightTotal += mapCoordinate.mapContainer.map.getHeightAt(mapCoordinate);
                heightTotal += cachedHeights[mapCoordinate];
            }
        }

        int diameter = radius * 2 + 1;

        return heightTotal / (diameter * diameter);
    }

    /*
     * Fog of War
     * */

    //Shader transparencyShader;
    //Shader unlitShader;

    private void SetupFogOfWar() {       
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

        float[,] fullOriginalFinalNoiseMap = mapGenerator.GetFullOriginalNoiseMap();

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

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, this); // height - 1 - y
                MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
                WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                Vector3 cubePosition = worldPosition.vector3;

                //fog.transform.position = cubePosition;// new Vector3((x * boxSizeX - halfTotalWidth) + boxSizeX / 2f, 0, (y * boxSizeZ - halfTotalHeight) + boxSizeZ / 2f);
                 
                fog.transform.SetParent(transform);
                fog.transform.position = new Vector3(cubePosition.x + xOffset, 1f, cubePosition.z + yOffset);
                fog.transform.localScale = new Vector3(1, 0.33f, 1); //new Vector3(boxSizeX * 11, 200, boxSizeZ * 11);
            }
        }
    }

    /*
     * Textures and Overhang Updates
     * */

    private void UpdateMaterialOverhangTextures() {

        Constants constants = Script.Get<Constants>();
        Material mapMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        //float[] leftList = new float[constants.layoutMapHeight + 2];
        //float[] rightList = new float[constants.layoutMapHeight + 2];
        //float[] topList = new float[constants.layoutMapWidth + 2];
        //float[] bottomList = new float[constants.layoutMapWidth + 2];

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


                //finalSampleHeight = neibours.topRightMap.map.getHeightAt(coordinate);
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


        //for(int y = 0; y < constants.layoutMapHeight; y++) {
        //    for(int x = 0; x < constants.layoutMapWidth; x++) {

        //        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, this);
        //        TerrainType terrain = map.GetTerrainAt(layoutCoordinate);

        //        textureIndexList[y * constants.layoutMapWidth + x] = texGen.RegionTypeTextureIndex(terrain.regionType);
        //    }
        //}

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

    /*
     * SelectionManagerDelegate Interface
     * */

    public void NotifyUpdateSelection(Selection selection) {
        Constants constants = Script.Get<Constants>();

        //Material mapMaterial = meshRenderer.material; meshRenderer Null??
        Material mapMaterial = GetComponent<MeshRenderer>().material;

        if(selection != null && selection.selectionType == Selection.SelectionType.Terrain && selection.coordinate.mapContainer == this) {
            //mapMaterial.SetFloat("selectedXOffsetLow", selection.coordinate.x * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
            //mapMaterial.SetFloat("selectedXOffsetHigh", (selection.coordinate.x + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

            //mapMaterial.SetFloat("selectedYOffsetLow", selection.coordinate.y * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
            //mapMaterial.SetFloat("selectedYOffsetHigh", (selection.coordinate.y + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));

            mapMaterial.SetFloat("hasSelection", 1);
            //mapMaterial.SetFloat("selectionX", selection.coordinate.x);
            //mapMaterial.SetFloat("selectionY", selection.coordinate.y);

            mapMaterial.SetVector("selection", new Vector2(selection.coordinate.x, selection.coordinate.y));

        } else {
            mapMaterial.SetFloat("hasSelection", 0);
            //mapMaterial.SetFloat("selectionX", -1);
            //mapMaterial.SetFloat("selectionY", -1);

            mapMaterial.SetVector("selection", new Vector2(-1, -1));

        }
    }

    /*
     * StatusEffectUpdateDelegate Interface
     * */

    public void StatusEffectMapUpdated(BuildingEffectStatus[,] statusMap, List<KeyValuePair<int, int>> effectedIndicies) {

        Constants constants = Script.Get<Constants>();

        TerrainManager terrainManager = Script.Get<TerrainManager>();
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        int width = constants.layoutMapWidth;
        int height = constants.layoutMapHeight;

        float boxSizeX = constants.featuresPerLayoutPerAxis;
        float boxSizeZ = constants.featuresPerLayoutPerAxis;

        float halfTotalWidth = boxSizeX * width / 2f;
        float halfTotalHeight = boxSizeZ * height / 2f;

        TimeManager timeManager = Script.Get<TimeManager>();

        int totalCouroutinesStarted = 0;
        Action createBoxEnded = () => {
            totalCouroutinesStarted--;

            if(totalCouroutinesStarted <= 0) {
                isBuildingBoxColliders = false;
            }
        };

        foreach(KeyValuePair<int, int> index in effectedIndicies) {
            int sampleX = index.Key;
            int sampleY = index.Value;

            int x = sampleX - (mapX * width);
            int y = sampleY - (mapY * height);

            if (x < 0 || x >= width || y < 0 || y >= height) {
                continue;
            }

            if((statusMap[sampleX, sampleY] & BuildingEffectStatus.Light) == BuildingEffectStatus.Light && fogOfWarMap[x, y]) {

                if(gameObject.activeInHierarchy == false) { gameObject.SetActive(true); }

                isBuildingBoxColliders = true;
                totalCouroutinesStarted++;

                LayoutCoordinate coordinate = new LayoutCoordinate(x, y, this);
                TerrainType terrainType = mapGenerator.KnownTerrainTypeAtIndex(sampleX, sampleY);

                TerraformTarget terraformTarget = new TerraformTarget(coordinate, terrainType);
                terraformTarget.percentage = 1.0f;

                Material material = fogOfWarMap[x, y].meshRenderer.material;

                Color color = material.color;
                //color.a = 0.0f;

                material.color = color;

                Action<int, float> fadeInBlock = (remainingTime, percentComplete) => {
                    color.a = percentComplete;
                    material.color = color;
                };

                Action<int, float> disolveBlock = (remainingTime, percentComplete) => {
                    material.SetFloat("_Cutoff", percentComplete);
                };

                int holdX = x;
                int holdY = y;

                Action fadeOutCompletion = () => {

                    fogOfWarMap[holdX, holdY].gameObject.SetActive(false);
                };

                Action boxEndAndAnimation = () => {
                    timeManager.AddNewTimer(fogOfWarFadeInDuration, fadeInBlock, () => {

                        // Do terraform to original terrain type
                        map.UpdateTerrainAtLocation(terraformTarget.coordinate, terraformTarget.terrainTypeTarget);
                        map.TerraformHeightMap(terraformTarget);

                        // Switch to disolve material
                        material = fogOfWarMap[holdX, holdY].disolveMaterial;
                        fogOfWarMap[holdX, holdY].meshRenderer.material = material;

                        // And disolve back in
                        timeManager.AddNewTimer(fogOfWarFadeOutDuration, disolveBlock, fadeOutCompletion, 3);
                    }, 3);

                    createBoxEnded();
                };

                StartCoroutine(CreateBoxCollidersAtCoordinate(new LayoutCoordinate(x, y, this), boxEndAndAnimation));
            }
        }
    }

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

    //private void OnDrawGizmos() {
    //    if (boxColliderArray == null) {
    //        return;
    //    }

    //    foreach (BoxCollider boxCollider in boxColliderArray) {

    //        float boxSizeX = map.featuresPerLayoutPerAxis * transform.lossyScale.x;
    //        float boxSizeZ = map.featuresPerLayoutPerAxis * transform.lossyScale.z;

    //        Gizmos.color = Color.cyan;
    //        Gizmos.DrawCube(boxCollider.center, boxCollider.size);
    //    }
    //}
}
