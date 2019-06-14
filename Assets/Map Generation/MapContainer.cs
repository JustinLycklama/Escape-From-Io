using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapContainer : MonoBehaviour
{
    public Map map;

    public int mapX, mapY; // Virtual position within the maps manager
    public Rect mapRect; // World position within the Maps Manager

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    BoxCollider[,] boxColliderArray;

    public void SetMapPosition(int mapX, int mapY, Rect mapRect) {
        this.mapX = mapX;
        this.mapY = mapY;
        this.mapRect = mapRect;
    }

    public void setMap(Map map) {

        if (this.map != null) {
            RemoveBoxColliders();
        }        

        this.map = map;
        map.mapContainer = this;

        DrawMesh();
        AddBoxColliders();
    }

    // TODO Remove
    public Map getMap() {
        return map;
    }

    public void DrawMesh() {
        if (meshFilter == null) {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        meshFilter.sharedMesh = map.meshData.FinalizeMesh();
        meshRenderer.sharedMaterial.mainTexture = map.meshTexture;
    }


    private void RemoveBoxColliders() {
        int width = boxColliderArray.GetLength(0);
        int height = boxColliderArray.GetLength(1);

        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                BoxCollider boxCollider = boxColliderArray[x,y];
                Destroy(boxCollider);
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
