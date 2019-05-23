using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map {
    public int mapWidth;
    public int mapHeight;
    public Vector2 textureMapSize;

    public MeshData meshData;
    public Texture2D meshTexture;

    public TerrainType[,] terrainData;
    float[,] heightMap;

    public Map(float[,] heightMap, int featuresPerLayoutPerAxis, MeshData meshData, Texture2D meshTexture, TerrainType[,] terrainData) {
        mapWidth = heightMap.GetLength(0);
       mapHeight = heightMap.GetLength(1);

        this.textureMapSize = new Vector2(mapWidth, mapHeight);
        
        //this.mapSize = new Vector2(mapWidth * featuresPerLayoutPerAxis, mapHeight * featuresPerLayoutPerAxis);

        this.heightMap = heightMap;
        this.meshData = meshData;
        this.meshTexture = meshTexture;
        this.terrainData = terrainData;
    }

    public float getHeightAt(MapCoordinate coordinate) {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);

        // If I have a map coordinate, should it be guarenteed to be on the map?
        //if (coordinate.x < 0 || coordinate.y < 0 || coordinate.x >= mapWidth || coordinate.y >= mapHeight) {
        //    return 0;
        //}

        // TODO: Triangle Calculations        
       
        return heightMap[coordinate.xLowSample, coordinate.yLowSample]; ;
    }

    //// Start is called before the first frame update
    //void Start() {

    //    meshFilter.sharedMesh = meshData.CreateMesh();
    //    meshRenderer.sharedMaterial.mainTexture = meshTexture;
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}
}
