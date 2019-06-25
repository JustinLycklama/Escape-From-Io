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

    /*
     * SelectionManagerDelegate Interface
     * */

    public void NotifyUpdateSelection(Selection selection) {
        Constants constants = Script.Get<Constants>();

        //Material mapMaterial = meshRenderer.material; meshRenderer Null??
        Material mapMaterial = GetComponent<MeshRenderer>().material;

        if(selection.selectionType == Selection.SelectionType.Terrain && selection.coordinate.mapContainer == this) {
            mapMaterial.SetFloat("selectedXOffsetLow", selection.coordinate.x * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
            mapMaterial.SetFloat("selectedXOffsetHigh", (selection.coordinate.x + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

            mapMaterial.SetFloat("selectedYOffsetLow", selection.coordinate.y * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
            mapMaterial.SetFloat("selectedYOffsetHigh", (selection.coordinate.y + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));

            mapMaterial.SetFloat("hasSelection", 1);
        } else {
            mapMaterial.SetFloat("hasSelection", 0);
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
