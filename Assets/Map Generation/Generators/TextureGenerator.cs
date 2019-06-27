using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TextureGenerator: MonoBehaviour {

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

    private void CreateSortedTypes() {
        MapGenerator mapGen = Script.Get<MapGenerator>();
        sortedTerrainTypes = mapGen.regions;

        System.Array.Sort(sortedTerrainTypes, delegate (TerrainType type1, TerrainType type2) {
            return type1.noiseBaseline.CompareTo(type2.noiseBaseline);
        });




    }

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    private TerrainType[] sortedTerrainTypes;




    public Texture2DArray GenerateTextureArray() {
        if (sortedTerrainTypes == null) {
            CreateSortedTypes();
        }

        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, sortedTerrainTypes.Length, textureFormat, true);
        for(int i = 0; i < sortedTerrainTypes.Length; i++) {
            textureArray.SetPixels(sortedTerrainTypes[i].texture.GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    public int RegionTypeTextureIndex(RegionType regionType) {
        if(sortedTerrainTypes == null) {
            CreateSortedTypes();
        }

        for(int i = 0; i < sortedTerrainTypes.Length; i++) {
            if(sortedTerrainTypes[i].regionType == regionType) {
                return i;
            }
        }

        return -1;        
    }

    public float[] TexturePriorityList() {
        if(sortedTerrainTypes == null) {
            CreateSortedTypes();
        }

        float[] priorityList = new float[sortedTerrainTypes.Length];

        for(int i = 0; i < sortedTerrainTypes.Length; i++) {
            priorityList[i] = sortedTerrainTypes[i].priority;
        }

        return priorityList;
    }
}
