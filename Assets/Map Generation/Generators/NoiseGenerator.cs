﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseData {
    public float scale;
    public int octaves;

    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;
}

public static class NoiseGenerator {

    // Octaves are detail levels
    // Persistance is the effect each subsequent octave has on the map as a whole 
    // Lacunarity is the increase in seperation between each samples point on each octave
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseData data) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(data.seed);

        Vector2[] octaveSampleOffsets = new Vector2[data.octaves];
        for (int i = 0; i < data.octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + data.offset.x;
            float offsetY = prng.Next(-100000, 100000) + data.offset.y;
            octaveSampleOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float scale = data.scale;
        if (scale <= 0) {
            scale = 0.0001f;
        }

        float minNoiseHeight = float.MaxValue;
        float maxNoiseHeight = float.MinValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {

                float ampliude = 1;
                float frequency = 1;

                // The total of all of our octaves to be assigned to the map
                float noiseHeight = 0;

                for (int i = 0; i < data.octaves; i++) {

                    // The farther apart the frequency, the more our height values will change 
                    float sampleX = (x - halfWidth) / scale * frequency + octaveSampleOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveSampleOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * ampliude;

                    ampliude *= data.persistance;
                    frequency *= data.lacunarity;
                }

                if (noiseHeight > maxNoiseHeight) {
                    maxNoiseHeight = noiseHeight;
                }

                if (noiseHeight < minNoiseHeight) {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;                
            }
        }

        // Normalize noise map
        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        
        return noiseMap;
    }
}