using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapContainerNeighbours {
    public MapContainer topMap, bottomMap, leftMap, rightMap;
    public MapContainer topLeftMap, topRightMap, bottomLeftMap, bottomRightMap;
}


public class MapContainer : MonoBehaviour, SelectionManagerDelegate
{
    public Map map;

    public int mapX, mapY; // Virtual position within the maps manager
    public Rect mapRect; // World position within the Maps Manager

    MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    BoxCollider[,] boxColliderArray;

    public MapContainerNeighbours neighbours = new MapContainerNeighbours();

    private void Start() {
        Script.Get<SelectionManager>().RegisterForNotifications(this);
    }

    private void OnDestroy() {
        Script.Get<SelectionManager>().EndNotifications(this);
    }

    public void SetMapPosition(int mapX, int mapY, Rect mapRect) {
        this.mapX = mapX;
        this.mapY = mapY;
        this.mapRect = mapRect;
    }

    public void setMap(Map map, bool withColliders = true) {

        if (this.map != null) {
            RemoveBoxColliders();
        }        

        this.map = map;
        map.mapContainer = this;

        map.CreateAllActionableItemOverrides();

        DrawMesh();

        if (withColliders == true) {
            AddBoxColliders();
        }

        SetupMaterialShader();
    }

    public void DrawMesh() {
        if (meshFilter == null) {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        meshFilter.sharedMesh = map.meshData.FinalizeMesh();
        meshRenderer.sharedMaterial.mainTexture = map.meshTexture;
    }

    public void UpdateMapOverhang() {
        MeshGenerator.UpdateMeshOverhang(map.meshData, neighbours);

        UpdateMaterialOverhangTextures();
        DrawMesh();
    }

    private void RemoveBoxColliders() {
        if(boxColliderArray != null) {
            int width = boxColliderArray.GetLength(0);
            int height = boxColliderArray.GetLength(1);

            for(int x = 0; x < width; x++) {
                for(int y = 0; y < height; y++) {
                    BoxCollider boxCollider = boxColliderArray[x, y];
                    DestroyImmediate(boxCollider);
                }
            }
        } else {
            BoxCollider[] colliders = GetComponents<BoxCollider>();
            foreach(BoxCollider collider in colliders) {
                DestroyImmediate(collider);
            }
        }       
    }

    private void AddBoxColliders() {

        int width = map.mapWidth / map.featuresPerLayoutPerAxis;
        int height = map.mapHeight / map.featuresPerLayoutPerAxis;

        float boxSizeX = map.featuresPerLayoutPerAxis;
        float boxSizeZ = map.featuresPerLayoutPerAxis;

        float halfTotalWidth = boxSizeX * width / 2f;
        float halfTotalHeight = boxSizeZ * height / 2f;

        boxColliderArray = new BoxCollider[width, height];

        for (int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, height - 1 - y, this);
                MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);

                boxCollider.center = new Vector3((x * boxSizeX - halfTotalWidth) + boxSizeX / 2f, 0 , (y * boxSizeZ - halfTotalHeight) + boxSizeZ / 2f);
                boxCollider.size = new Vector3(boxSizeX, map.getHeightAt(mapCoordinate) * 2, boxSizeZ);

                boxColliderArray[x,y] = boxCollider;
            }
        }
    }

    public void ResizeBoxColliderAt(LayoutCoordinate layoutCoordinate) {
        float boxSizeX = map.featuresPerLayoutPerAxis;
        float boxSizeZ = map.featuresPerLayoutPerAxis;

        MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);

        BoxCollider boxCollider = boxColliderArray[layoutCoordinate.x, (map.mapHeight / map.featuresPerLayoutPerAxis) - 1 - layoutCoordinate.y];

        boxCollider.size = new Vector3(boxSizeX, map.getHeightAt(mapCoordinate) * 2, boxSizeZ);
    }

    private void UpdateMaterialOverhangTextures() {
        Constants constants = Script.Get<Constants>();
        Material mapMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        float[] leftList = new float[constants.layoutMapHeight + 2];
        float[] rightList = new float[constants.layoutMapHeight + 2];
        float[] topList = new float[constants.layoutMapWidth + 2];
        float[] bottomList = new float[constants.layoutMapWidth + 2];

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

            if(y > 0 && y < constants.layoutMapHeight - 1 && neighbours.leftMap != null) {
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

        // Bottom

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

    float[] textureIndexList;

    public void SetupMaterialShader() {
        Constants constants = Script.Get<Constants>();
        Material mapMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        mapMaterial.SetFloat("tileSize", constants.featuresPerLayoutPerAxis);

        int mapWidthWithOverhang = constants.layoutMapWidth + 2;
        int mapHeightWithOverhang = constants.layoutMapHeight + 2;

        mapMaterial.SetFloat("mapLayoutWidth", mapWidthWithOverhang);
        mapMaterial.SetFloat("mapLayoutHeight", mapHeightWithOverhang);


        mapMaterial.SetFloat("mapXOffsetLow", 0 - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
        mapMaterial.SetFloat("mapXOffsetHigh", constants.layoutMapWidth - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

        mapMaterial.SetFloat("mapYOffsetLow", 0 - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
        mapMaterial.SetFloat("mapYOffsetHigh", constants.layoutMapHeight - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));

        TextureGenerator texGen = Script.Get<TextureGenerator>();

        UpdateShaderTerrainTextures();

        mapMaterial.SetFloatArray("indexPriority", texGen.TexturePriorityList());
        mapMaterial.SetFloatArray("indexScale", texGen.TextureScaleList());

        Texture2DArray texturesArray = texGen.TextureArray();
        mapMaterial.SetTexture("baseTextures", texturesArray);
    }

    public void UpdateShaderTerrainTextures() {
        Constants constants = Script.Get<Constants>();
        Material mapMaterial = GetComponent<MeshRenderer>().sharedMaterial;

        int mapWidthWithOverhang = constants.layoutMapWidth + 2;
        int mapHeightWithOverhang = constants.layoutMapHeight + 2;

        textureIndexList = new float[mapWidthWithOverhang * mapHeightWithOverhang];
        TextureGenerator texGen = Script.Get<TextureGenerator>();

        for(int y = 0; y < mapHeightWithOverhang; y++) {
            for(int x = 0; x < mapWidthWithOverhang; x++) {

                if(x == 0 || x == mapWidthWithOverhang - 1 || y == 0 || y == mapHeightWithOverhang - 1) {
                    textureIndexList[y * mapWidthWithOverhang + x] = -1;
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

        if(selection.selectionType == Selection.SelectionType.Terrain && selection.coordinate.mapContainer == this) {
            //mapMaterial.SetFloat("selectedXOffsetLow", selection.coordinate.x * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
            //mapMaterial.SetFloat("selectedXOffsetHigh", (selection.coordinate.x + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

            //mapMaterial.SetFloat("selectedYOffsetLow", selection.coordinate.y * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
            //mapMaterial.SetFloat("selectedYOffsetHigh", (selection.coordinate.y + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));

            mapMaterial.SetFloat("hasSelection", 1);
            mapMaterial.SetFloat("selectionX", selection.coordinate.x);
            mapMaterial.SetFloat("selectionY", selection.coordinate.y);

        } else {
            mapMaterial.SetFloat("hasSelection", 0);
            mapMaterial.SetFloat("selectionX", -1);
            mapMaterial.SetFloat("selectionY", -1);
        }
    }


    //private void OnDrawGizmos() {
    //    if (boxColliderArray == null) {
    //        return;
    //    }

    //    foreach (BoxCollider boxCollider in boxColliderArray) {

    //        float boxSizeX = map.featuresPerLayoutPerAxis * transform.localScale.x;
    //        float boxSizeZ = map.featuresPerLayoutPerAxis * transform.localScale.z;

    //        Gizmos.color = Color.cyan;
    //        Gizmos.DrawCube(boxCollider.center, boxCollider.size);
    //    }
    //}
}
