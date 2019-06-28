using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RegionType { Water, Land, Mountain, Path }

[System.Serializable]
public struct TerrainType {
    public string name;

    public float noiseBaseline;
    public float noiseMax;

    public float walkSpeedMultiplier;
    public bool walkable;
    public RegionType regionType;

    public Color color;
    public bool plateau;
    public bool plateauAtBase;

    public Texture2D texture;
    public Color tint;
    [Range(0, 1)]
    public float tintStrength;
    public float textureScale;
    public int priority;

    public override bool Equals(object obj) {
        return base.Equals(obj);
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }

    public override string ToString() {
        return regionType.ToString();
    }

    public bool ValueIsMember(float value) {
        return value <= noiseMax && value >= noiseBaseline;
    }

    public static bool operator ==(TerrainType obj1, TerrainType obj2) {
        return obj1.regionType == obj2.regionType;
    }

    public static bool operator !=(TerrainType obj1, TerrainType obj2) {
        return !(obj1 == obj2);
    }
}

public class MapGenerator : MonoBehaviour {

    public enum DrawMode {NoiseMap, ColorMap, Mesh}
    public DrawMode drawMode;

    private int layoutMapWidth;
    private int layoutMapHeight;

    public MapContainer demoMapContainer;

    public NoiseData layoutMapNoiseData;
    public NoiseData groundFeaturesMapNoiseData;
    public NoiseData mountainFeaturesMapNoiseData;

    [Range(0, 1)]
    public float groundFeaturesImpactOnLayout;

    [Range(0, 2)]
    public float mountainFeaturesImpactOnLayout;

    private int featuresPerLayoutPerAxis;    

    public bool autoUpdate;

    public TerrainType[] regions;

    public TerrainType TerrainForRegion(RegionType regionType) {
        foreach (TerrainType type in regions) {
            if (type.regionType == regionType) {
                return type;
            }
        }

        return regions[0];
    }

    public GameObject mapObject;

    private void OnValidate() {
        if(layoutMapWidth < 1) {
            layoutMapWidth = 1;
        }

        if(layoutMapHeight < 1) {
            layoutMapHeight = 1;
        }
    }

    public float[,] GenerateLayoutMap(int width, int length) {
        return NoiseGenerator.GenerateNoiseMap(width, length, layoutMapNoiseData);
    }

    public float[,] GenerateGroundFeaturesMap(int width, int length) {
        return NoiseGenerator.GenerateNoiseMap(width, length, groundFeaturesMapNoiseData);
    }

    public float[,] GenerateMountainFeaturesMap(int width, int length) {
        return NoiseGenerator.GenerateNoiseMap(width, length, mountainFeaturesMapNoiseData);
    }

    public Map GenerateMap(MapContainer mapContainer, float[,] layoutNoiseMap, float[,] groundFeaturesNoiseMap, float[,] mountainFeaturesNoiseMap) {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        this.layoutMapWidth = layoutNoiseMap.GetLength(0);
        this.layoutMapHeight = layoutNoiseMap.GetLength(1);
        this.featuresPerLayoutPerAxis = constants.featuresPerLayoutPerAxis;

        // First generate a small noise map, use for the general layout (eg. which area is a path, which is a rock, ...)
        // This is the noise map the grid and selectons will use
        //float[,] layoutNoiseMap = NoiseGenerator.GenerateNoiseMap(layoutMapWidth, layoutMapHeight, layoutMapNoiseData);

        int noiseMapWidth = groundFeaturesNoiseMap.GetLength(0);
        int noiseMapHeight = groundFeaturesNoiseMap.GetLength(1);

        // Then generate a larger scale noise map, and overlay it on the small one
        //float[,] groundFeaturesNoiseMap = NoiseGenerator.GenerateNoiseMap(noiseMapWidth, noiseMapHeight, groundFeaturesMapNoiseData);
        //float[,] mountainFeaturesNoiseMap = NoiseGenerator.GenerateNoiseMap(noiseMapWidth, noiseMapHeight, mountainFeaturesMapNoiseData);

        TerrainType[,] terrainMap = PlateauMap(layoutNoiseMap);
        float[,] noiseMap = CreateMapWithFeatures(layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap, terrainMap);

        NormalizeMap(noiseMap);

        TextureGenerator textureGenerator = Script.Get<TextureGenerator>();

        Map map = new Map(noiseMap, layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap,
            featuresPerLayoutPerAxis,
            MeshGenerator.GenerateTerrainMesh(noiseMap, featuresPerLayoutPerAxis),
            textureGenerator.TextureFromColorMap(CreateColorMapWithTerrain(noiseMap, terrainMap, mapContainer), noiseMapWidth + 2, noiseMapHeight + 2),
            terrainMap
            );

        MapDebugDisplay debugDisplay = FindObjectOfType<MapDebugDisplay>();
        switch (drawMode) {
            case DrawMode.NoiseMap:
                if (debugDisplay != null) {
                    debugDisplay.DrawTexture(textureGenerator.TextureFromNoiseMap(noiseMap));
                    debugDisplay.DrawTextures(textureGenerator.TextureFromNoiseMap(layoutNoiseMap), textureGenerator.TextureFromNoiseMap(groundFeaturesNoiseMap));
                }                
                break;
            case DrawMode.ColorMap:
                if(debugDisplay != null) {

                    debugDisplay.DrawTexture(textureGenerator.TextureFromColorMap(CreateColorMap(noiseMap), noiseMap.GetLength(0), noiseMap.GetLength(1)));
                    debugDisplay.DrawTextures(textureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                        textureGenerator.TextureFromColorMap(CreateColorMap(groundFeaturesNoiseMap), groundFeaturesNoiseMap.GetLength(0), groundFeaturesNoiseMap.GetLength(1))
                        );
                }
                break;
            case DrawMode.Mesh:
                mapContainer.setMap(map, false);

                if(debugDisplay != null) {
                        debugDisplay.DrawMeshes(MeshGenerator.GenerateTerrainMesh(layoutNoiseMap, 1), textureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                        MeshGenerator.GenerateTerrainMesh(groundFeaturesNoiseMap, 1), textureGenerator.TextureFromColorMap(CreateColorMap(groundFeaturesNoiseMap), groundFeaturesNoiseMap.GetLength(0), groundFeaturesNoiseMap.GetLength(1))
                        );
                }
                break;
        }
       
        return map;
    }

    public float[,] TerraformHeightMap(float[,] layoutNoiseMap, float[,] groundFeaturesNoiseMap, float[,] mountainFeaturesNoiseMap, TerrainType[,] terrainMap, float currentLayoutHeight, LayoutCoordinate coordinate) {
        // TODO More interesting interpolations to mimic mining

        layoutNoiseMap[coordinate.x, coordinate.y] = currentLayoutHeight;

        float[,] noiseMap = CreateMapWithFeatures(layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap, terrainMap);
        NormalizeMap(noiseMap);

        return noiseMap;
    }

    // PRIVATE

    private float[,] CreateMapWithFeatures(float[,] layoutMap, float[,] groundFeaturesMap, float[,] mountainFeaturesMap, TerrainType[,] terrainMap) {
        int featuresWidth = groundFeaturesMap.GetLength(0);
        int featuresHeight = groundFeaturesMap.GetLength(1);

        int dipRadius = 3;

        float[,] fullMap = new float[featuresWidth, featuresHeight];
        for(int y = 0; y < featuresWidth; y++) {
            for(int x = 0; x < featuresHeight; x++) {

                int sampleX = x / featuresPerLayoutPerAxis;
                int sampleY = y / featuresPerLayoutPerAxis;

                TerrainType thisTerrainType = terrainMap[sampleX, sampleY];

                switch(thisTerrainType.regionType) {
                    case RegionType.Water:
                        fullMap[x, y] = layoutMap[sampleX, sampleY];
                        break;
                    case RegionType.Land:
                        fullMap[x, y] = (layoutMap[sampleX, sampleY]) * (1 - groundFeaturesImpactOnLayout) + ((groundFeaturesMap[x, y] * groundFeaturesImpactOnLayout));
                        break;
                    case RegionType.Mountain:
                        fullMap[x, y] = layoutMap[sampleX, sampleY] + (mountainFeaturesMap[x, y] * mountainFeaturesImpactOnLayout);
                        break;
                }

                float distanceToEdgeX = x % featuresPerLayoutPerAxis;
                float distanceToEdgeY = y % featuresPerLayoutPerAxis;

                fullMap[x, y] = SampleAtXY(x, y, layoutMap, groundFeaturesMap, mountainFeaturesMap, terrainMap);

                for(int i = 0; i < dipRadius; i++) {

                    float baseline = thisTerrainType.noiseBaseline * (dipRadius - (i + 1)) / dipRadius;                    

                    // Left
                    if((sampleX == 0) || (sampleX - 1 >= 0 && thisTerrainType.noiseBaseline > terrainMap[sampleX - 1, sampleY].noiseBaseline)) {
                        if (distanceToEdgeX == i) {
                            fullMap[x, y] = baseline + (fullMap[x, y] * ((i + 1)) / dipRadius);
                            continue;
                        }
                    }

                    // Top
                    if((sampleY == 0) || (sampleY - 1 >= 0 && thisTerrainType.noiseBaseline > terrainMap[sampleX, sampleY - 1].noiseBaseline)) {
                        if(distanceToEdgeY == i) {
                            fullMap[x, y] = baseline + (fullMap[x, y] * ((i + 1)) / dipRadius);
                            continue;
                        }
                    }

                    // Right
                    if( (sampleX + 1 == terrainMap.GetLength(0)) || (sampleX + 1 < terrainMap.GetLength(0) && thisTerrainType.noiseBaseline > terrainMap[sampleX + 1, sampleY].noiseBaseline)) {
                        if((featuresPerLayoutPerAxis - distanceToEdgeX - 1) == i) {
                            fullMap[x, y] = baseline + (fullMap[x, y] * ((i + 1)) / dipRadius);
                            continue;
                        }
                    }

                    //// Bottom
                    if((sampleY + 1 == terrainMap.GetLength(1)) ||(sampleY + 1 < terrainMap.GetLength(1) && thisTerrainType.noiseBaseline > terrainMap[sampleX, sampleY + 1].noiseBaseline)) {
                        if(featuresPerLayoutPerAxis - distanceToEdgeY - 1 == i) {
                            fullMap[x, y] = baseline + (fullMap[x, y] * ((i + 1)) / dipRadius);
                            continue;
                        }
                    }
                }


                //if (distanceToEdgeX == 0 || (distanceToEdgeX == featuresPerLayoutPerAxis) || distanceToEdgeY == 0 || (distanceToEdgeY == featuresPerLayoutPerAxis)) {
                //    fullMap[x, y] *= 0.30f;
                //}

                //if(distanceToEdgeX == 1 || (distanceToEdgeX == featuresPerLayoutPerAxis - 1) || distanceToEdgeY == 1 || (distanceToEdgeY == featuresPerLayoutPerAxis - 1)) {
                //    fullMap[x, y] *= 0.60f;
                //}

                //if(distanceToEdgeX == 2 || (distanceToEdgeX == featuresPerLayoutPerAxis - 2) || distanceToEdgeY == 2 || (distanceToEdgeY == featuresPerLayoutPerAxis - 2)) {
                //    fullMap[x, y] *= 0.90f;
                //}
            }
        }

        return fullMap;
    }

    private float SampleAtXY(int x, int y, float[,] layoutMap, float[,] groundFeaturesMap, float[,] mountainFeaturesMap, TerrainType[,] terrainMap) {
        int sampleX = x / featuresPerLayoutPerAxis;
        int sampleY = y / featuresPerLayoutPerAxis;

        switch(terrainMap[sampleX, sampleY].regionType) {
            case RegionType.Water:
                return layoutMap[sampleX, sampleY];
            case RegionType.Land:
                return (layoutMap[sampleX, sampleY]) + ((groundFeaturesMap[x, y] * groundFeaturesImpactOnLayout));
            case RegionType.Mountain:
                return layoutMap[sampleX, sampleY] + (mountainFeaturesMap[x, y] * mountainFeaturesImpactOnLayout);
        }

        return 0;
    }

    // TEXTURES

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

    private Color[] CreateColorMapWithTerrain(float[,] noiseMap, TerrainType[,] terrainTypeMap, MapContainer mapContainer) {

        int noiseMapWidth = noiseMap.GetLength(0);
        int noiseMapHeight = noiseMap.GetLength(1);

        //  + 2 for overhang
        int colorMapWidth = noiseMapWidth + 2;
        int colorMapHeight = noiseMapHeight + 2;

        int terrainMapWidth = terrainTypeMap.GetLength(0);
        int terrainMapHeight = terrainTypeMap.GetLength(1);

        Color[] colorMap = new Color[(colorMapWidth) * (colorMapHeight)];
        for(int y = 0; y < colorMapWidth; y++) {
            for(int x = 0; x < colorMapHeight; x++) {

                int boundedX = x - 1;
                int boundedY = y - 1;

                Color colorAtIndex = Color.white;
                if(!(boundedX < 0 || boundedX > noiseMapWidth - 1 || boundedY < 0 || boundedY > noiseMapHeight - 1)) {                   
                    int finalSampleX = boundedX / (noiseMapWidth / terrainMapWidth);
                    int finalSampleY = boundedY / (noiseMapHeight / terrainMapHeight);

                    colorAtIndex = terrainTypeMap[finalSampleX, finalSampleY].color;

                    if((boundedX + 1) % featuresPerLayoutPerAxis == 0 || (boundedY + 1) % featuresPerLayoutPerAxis == 0) {
                        // If the next terrain is higher, use that color index instead
                        if((boundedX + 1) % featuresPerLayoutPerAxis == 0 && (finalSampleX + 1) < layoutMapHeight - 1) {
                            if(terrainTypeMap[finalSampleX + 1, finalSampleY].noiseBaseline > terrainTypeMap[finalSampleX, finalSampleY].noiseBaseline) {
                                colorAtIndex = terrainTypeMap[finalSampleX + 1, finalSampleY].color;
                            }
                        }

                        if ((boundedY + 1) % featuresPerLayoutPerAxis == 0 && (finalSampleY + 1) < layoutMapHeight - 1) {
                            if (terrainTypeMap[finalSampleX, finalSampleY + 1].noiseBaseline > terrainTypeMap[finalSampleX, finalSampleY].noiseBaseline) {
                                colorAtIndex = terrainTypeMap[finalSampleX, finalSampleY + 1].color;
                            }
                        }

                        colorAtIndex *= 2;
                    }
                }

                colorMap[y * colorMapWidth + x] = colorAtIndex;
            }
        }

        return colorMap;
    }

    // Returns a Map of terrainTypes
    private TerrainType[,] PlateauMap(float[,] map) {
        int mapWidth = map.GetLength(0);
        int mapHeight = map.GetLength(1);

        TerrainType[,] terrainMap = new TerrainType[map.GetLength(0), map.GetLength(1)];

        for(int y = 0; y < mapHeight; y++) {
            for(int x = 0; x < mapWidth; x++) {

                for(int i = 0; i < regions.Length; i++) {
                    if(regions[i].plateau && regions[i].ValueIsMember(map[x, y])) {

                        map[x, y] = (regions[i].plateauAtBase ? regions[i].noiseBaseline + 0.001f : regions[i].noiseMax - 0.001f);
                        terrainMap[x, y] = regions[i];
                        break;
                    }
                }
            }
        }

        return terrainMap;
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
}
