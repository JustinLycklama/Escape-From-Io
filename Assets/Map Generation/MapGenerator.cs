using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {


    public enum DrawMode {NoiseMap, ColorMap, Mesh}
    public DrawMode drawMode;

    public int layoutMapWidth;
    public int layoutMapHeight;

    public NoiseData layoutMapNoiseData;
    public NoiseData featuresMapNoiseData;

    [Range(0, 1)]
    public float featuresImpactOnLayout;
    public int featuresPerLayoutPerAxis = 10;    

    public bool autoUpdate;

    public TerrainType[] regions;

    public void GenerateMap() {
        // First generate a small noise map, use for the general layout (eg. which area is a path, which is a rock, ...)
        // This is the noise map the grid and selectons will use
        float[,] layoutNoiseMap = NoiseGenerator.GenerateNoiseMap(layoutMapWidth, layoutMapHeight, layoutMapNoiseData);

        int noiseMapWidth = layoutMapWidth * featuresPerLayoutPerAxis;
        int noiseMapHeight = layoutMapHeight * featuresPerLayoutPerAxis;

        // Then generate a larger scale noise map, and overlay it on the small one
        float[,] featuresNoiseMap = NoiseGenerator.GenerateNoiseMap(noiseMapWidth, noiseMapHeight, featuresMapNoiseData);

        PlateauMap(layoutNoiseMap);
        float[,] noiseMap = CreateMapWithFeatures(layoutNoiseMap, featuresNoiseMap);

        NormalizeMap(noiseMap);


        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode) {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromNoiseMap(noiseMap));
                display.DrawTextures(TextureGenerator.TextureFromNoiseMap(layoutNoiseMap), TextureGenerator.TextureFromNoiseMap(featuresNoiseMap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(CreateColorMap(noiseMap), noiseMap.GetLength(0), noiseMap.GetLength(1)));
                display.DrawTextures(TextureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                    TextureGenerator.TextureFromColorMap(CreateColorMap(featuresNoiseMap), featuresNoiseMap.GetLength(0), featuresNoiseMap.GetLength(1))
                    );

                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.TextureFromColorMap(CreateColorMap(noiseMap), noiseMapWidth, noiseMapHeight));
                //display.DrawMeshes(MeshGenerator.GenerateTerrainMesh(layoutNoiseMap), TextureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                //    MeshGenerator.GenerateTerrainMesh(featuresNoiseMap), TextureGenerator.TextureFromColorMap(CreateColorMap(featuresNoiseMap), featuresNoiseMap.GetLength(0), featuresNoiseMap.GetLength(1))
                //    );

                break;
        }
    }

    private Color[] CreateColorMap(float[,] noiseMap) {

        int noiseMapWidth = noiseMap.GetLength(0);
        int noiseMapHeight = noiseMap.GetLength(1);

        Color[] colorMap = new Color[noiseMapWidth * noiseMapHeight];
        for(int y = 0; y < noiseMapHeight; y++) {
            for(int x = 0; x < noiseMapWidth; x++) {

                float currentHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++) {
                    if(regions[i].ValueIsMember(currentHeight)) {
                        colorMap[y * noiseMapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return colorMap;
    }

    private void PlateauMap(float[,] map) {
        int mapWidth = map.GetLength(0);
        int mapHeight = map.GetLength(1);

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {

                for(int i = 0; i < regions.Length; i++) {
                    if(regions[i].plateau && regions[i].ValueIsMember(map[x, y])) {

                        map[x, y] = (regions[i].plateauAtBase ? regions[i].noiseBaseline + 0.001f : regions[i].noiseMax - 0.001f);
                        break;
                    }
                }
            }
        }
    }

    private float[,] CreateMapWithFeatures(float[,] layoutMap, float[,] featuresMap) {
        int featuresWidth = featuresMap.GetLength(0);
        int featuresHeight = featuresMap.GetLength(1);

        float[,] fullMap = new float[featuresWidth, featuresHeight];
        for(int y = 0; y < featuresWidth; y++) {
            for(int x = 0; x < featuresHeight; x++) {
                int sampleX = x / featuresPerLayoutPerAxis;
                int sampleY = y / featuresPerLayoutPerAxis;

                fullMap[x, y] = layoutMap[sampleX, sampleY] + (featuresMap[x, y] * featuresImpactOnLayout);                
            }
        }

        return fullMap;
    }

    private void NormalizeMap(float[,] noiseMap) {
        int mapWidth = noiseMap.GetLength(0);
        int mapHeight = noiseMap.GetLength(1);

        float minNoiseHeight = float.MaxValue;
        float maxNoiseHeight = float.MinValue;

        for(int y = 0; y < mapWidth; y++) {
            for(int x = 0; x < mapHeight; x++) {
                int sampleX = x / featuresPerLayoutPerAxis;
                int sampleY = y / featuresPerLayoutPerAxis;

                float noiseHeight = noiseMap[x, y];

                if(noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                }

                if(noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }
            }
        }

        // Normalize noise map
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
    }

    private void OnValidate() {
        if(layoutMapWidth < 1) {
            layoutMapWidth = 1;
        }

        if(layoutMapHeight < 1) {
            layoutMapHeight = 1;
        }

        //if (lacunarity < 1) {
        //    lacunarity = 1;
        //}

        //if (octaves < 0) {
        //    octaves = 0;
        //}
    }
}

// System.Serializable shows up in inspector
[System.Serializable]
public struct TerrainType {
    public string name;

    public float noiseBaseline;
    public float noiseMax;

    public Color color;
    public bool plateau;
    public bool plateauAtBase;

    public bool ValueIsMember(float value) {
        return value <= noiseMax && value >= noiseBaseline;
    }
}
