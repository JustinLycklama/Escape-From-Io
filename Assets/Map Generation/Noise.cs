using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

    // Octaves are detail levels
    // Persistance is the effect each subsequent octave has on the map as a whole 
    // Lacunarity is the increase in seperation between each samples point on each octave
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);

        Vector2[] octaveSampleOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveSampleOffsets[i] = new Vector2(offsetX, offsetY);
        }

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

                for (int i = 0; i < octaves; i++) {

                    // The farther apart the frequency, the more our height values will change 
                    float sampleX = (x - halfWidth) / scale * frequency + octaveSampleOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveSampleOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * ampliude;

                    ampliude *= persistance;
                    frequency *= lacunarity;
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
