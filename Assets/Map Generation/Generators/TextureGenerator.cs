using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TextureGenerator : MonoBehaviour {

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    //private Color[] CreateColorMapWithTerrain(TerrainType[,] terrainTypeMap, MapContainer mapContainer) {

    //    int terrainMapWidth = terrainTypeMap.GetLength(0);
    //    int terrainMapHeight = terrainTypeMap.GetLength(1);

    //    //  + 2 for overhang
    //    int colorMapWidth = terrainMapWidth * textureSize;
    //    int colorMapHeight = terrainMapHeight * textureSize;

    //    Color[] colorMap = new Color[(colorMapWidth) * (colorMapHeight)];
    //    for(int y = 0; y < colorMapWidth; y++) {
    //        for(int x = 0; x < colorMapHeight; x++) {

    //            int boundedX = x - 1;
    //            int boundedY = y - 1;

    //            Color colorAtIndex = Color.white;
    //            if(!(boundedX < 0 || boundedX > noiseMapWidth - 1 || boundedY < 0 || boundedY > noiseMapHeight - 1)) {
    //                int finalSampleX = boundedX / (noiseMapWidth / terrainMapWidth);
    //                int finalSampleY = boundedY / (noiseMapHeight / terrainMapHeight);

    //                colorAtIndex = terrainTypeMap[finalSampleX, finalSampleY].color;

    //                if((boundedX + 1) % featuresPerLayoutPerAxis == 0 || (boundedY + 1) % featuresPerLayoutPerAxis == 0) {
    //                    // If the next terrain is higher, use that color index instead
    //                    if((boundedX + 1) % featuresPerLayoutPerAxis == 0 && (finalSampleX + 1) < layoutMapHeight - 1) {
    //                        if(terrainTypeMap[finalSampleX + 1, finalSampleY].noiseBaseline > terrainTypeMap[finalSampleX, finalSampleY].noiseBaseline) {
    //                            colorAtIndex = terrainTypeMap[finalSampleX + 1, finalSampleY].color;
    //                        }
    //                    }

    //                    if((boundedY + 1) % featuresPerLayoutPerAxis == 0 && (finalSampleY + 1) < layoutMapHeight - 1) {
    //                        if(terrainTypeMap[finalSampleX, finalSampleY + 1].noiseBaseline > terrainTypeMap[finalSampleX, finalSampleY].noiseBaseline) {
    //                            colorAtIndex = terrainTypeMap[finalSampleX, finalSampleY + 1].color;
    //                        }
    //                    }

    //                    colorAtIndex *= 2;
    //                }
    //            }

    //            colorMap[y * colorMapWidth + x] = colorAtIndex;
    //        }
    //    }

    //    return colorMap;
    //}

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
