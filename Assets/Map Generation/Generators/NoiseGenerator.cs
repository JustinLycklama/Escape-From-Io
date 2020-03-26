using System.Collections;
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


    private static System.Random _random = null;
    public static System.Random random {
        get {
            if(_random == null) {
                int seed = System.Guid.NewGuid().GetHashCode();
                seed = 1904886234;

                MonoBehaviour.print("Seed: " + seed);

                _random = new System.Random(seed);
            }

            return _random;
        }
    }

    public static float[,] GenerateRandomNoiseMap(int mapWidth, int mapHeight) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random rnd = new System.Random(System.Guid.NewGuid().GetHashCode());

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {

                noiseMap[x, y] = rnd.Next(0, 100);
            }
        }

        return noiseMap;
    }

    // Octaves are detail levels
    // Persistance is the effect each subsequent octave has on the map as a whole 
    // Lacunarity is the increase in seperation between each samples point on each octave
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseData data) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Use constant seed. NoiseData.seed will do nothing
        System.Random prng = NoiseGenerator.random;

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

    public struct MinMaxofNormalize {
        public float min;
        public float max;

        public MinMaxofNormalize(float min, float max) {
            this.min = min;
            this.max = max;
        }
    }

    public static MinMaxofNormalize NormalizeMap(float[,] noiseMap) {
        int mapWidth = noiseMap.GetLength(0);
        int mapHeight = noiseMap.GetLength(1);

        float minNoiseHeight = float.MaxValue;
        float maxNoiseHeight = float.MinValue;

        for(int y = 0; y < mapWidth; y++) {
            for(int x = 0; x < mapHeight; x++) {
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

        return new MinMaxofNormalize(minNoiseHeight, maxNoiseHeight);
    }
}
