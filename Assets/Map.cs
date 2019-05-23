using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map {
    public Vector2 mapSize;
    public Vector2 textureMapSize;

    public MeshData meshData;
    public Texture2D meshTexture;

    public TerrainType[,] terrainData;

    public Map(int mapWidth, int mapHeight, int featuresPerLayoutPerAxis, MeshData meshData, Texture2D meshTexture, TerrainType[,] terrainData) {
        this.textureMapSize = new Vector2(mapWidth, mapHeight);
        this.mapSize = new Vector2(mapWidth * featuresPerLayoutPerAxis, mapHeight * featuresPerLayoutPerAxis);        
        this.meshData = meshData;
        this.meshTexture = meshTexture;
        this.terrainData = terrainData;
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
