using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TextureGenerator : MonoBehaviour {

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Texture2D TextureFromColorMap(Color[] colorMap, int width, int height) {
        Texture2D texture = new Texture2D(width, height);

        texture.SetPixels(colorMap);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return texture;
    }

    public Texture2D TextureFromNoiseMap(float[,] map) {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, map[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }

    private Texture2DArray textureArray;
    private void GenerateTextureArray() {
        TerrainManager terrainManager = Script.Get<TerrainManager>();
        textureArray = new Texture2DArray(textureSize, textureSize, terrainManager.terrainTypes.Length, textureFormat, true);
        for(int i = 0; i < terrainManager.terrainTypes.Length; i++) {
            textureArray.SetPixels(terrainManager.terrainTypes[i].texture.GetPixels(), i);
        }
        textureArray.Apply();
    }

    public Texture2DArray TextureArray() {
        if (textureArray == null) {
            GenerateTextureArray();
        }

        return textureArray;
    }

    private Texture2DArray bumpMapArray;
    private void GenerateBumpMapArray() {
        TerrainManager terrainManager = Script.Get<TerrainManager>();
        bumpMapArray = new Texture2DArray(textureSize, textureSize, terrainManager.terrainTypes.Length, textureFormat, true);
        for(int i = 0; i < terrainManager.terrainTypes.Length; i++) {
            bumpMapArray.SetPixels(terrainManager.terrainTypes[i].bumpMap.GetPixels(), i);
        }
        bumpMapArray.Apply();
    }

    public Texture2DArray BumpMapArray() {
        if(bumpMapArray == null) {
            GenerateBumpMapArray();
        }

        return bumpMapArray;
    }

    public int RegionTypeTextureIndex(TerrainType terrainType) {
        TerrainManager terrainManager = Script.Get<TerrainManager>();


        for(int i = 0; i < terrainManager.terrainTypes.Length; i++) {
            if(terrainManager.terrainTypes[i] == terrainType) {
                return i;
            }
        }

        return -1;        
    }

    public float[] TexturePriorityList() {
        TerrainManager terrainManager = Script.Get<TerrainManager>();

        float[] priorityList = new float[terrainManager.terrainTypes.Length];

        for(int i = 0; i < terrainManager.terrainTypes.Length; i++) {
            priorityList[i] = terrainManager.terrainTypes[i].priority;
        }

        return priorityList;
    }

    public float[] TextureScaleList() {
        TerrainManager terrainManager = Script.Get<TerrainManager>();

        float[] priorityList = new float[terrainManager.terrainTypes.Length];

        for(int i = 0; i < terrainManager.terrainTypes.Length; i++) {
            priorityList[i] = terrainManager.terrainTypes[i].textureScale;
        }

        return priorityList;
    }
}
