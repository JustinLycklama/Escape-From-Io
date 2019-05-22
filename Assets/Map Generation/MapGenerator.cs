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

    [Range(0, 2)]
    public float featuresImpactOnLayout;
    public static int featuresPerLayoutPerAxis = 10;    

    public bool autoUpdate;

    public TerrainType[] regions;

    struct MapPoint {
        public int virtualX;
        public int virtualY;

        public float worldX;
        public float worldY;

        public MapPoint(float worldX, float worldY) {
            this.worldX = worldX;
            this.worldY = worldY;

            virtualX = (int)worldX / featuresPerLayoutPerAxis;
            virtualY = (int)worldY / featuresPerLayoutPerAxis;
        }
    }

    private MapPoint selectedPoint = new MapPoint(0,0);

    public void Awake() {
        GenerateMap();
    }

    public void Update() {
        if(Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit)) {




                MapDisplay display = FindObjectOfType<MapDisplay>();

                Transform mapMeshSpace = display.meshRenderer.transform;
                Vector3 selectionPoint = hit.point;

                Vector3 selectedPointOnMap = mapMeshSpace.InverseTransformPoint(selectionPoint);

                Debug.Log("Mouse Down Hit the following object: " + selectedPointOnMap);

                selectedPoint = new MapPoint(selectedPointOnMap.x + (layoutMapWidth * featuresPerLayoutPerAxis / 2f), (-selectedPointOnMap.z) + (layoutMapHeight * featuresPerLayoutPerAxis / 2f));
                GenerateMap();
            }

        }
    }

    public void GenerateMap() {
        // First generate a small noise map, use for the general layout (eg. which area is a path, which is a rock, ...)
        // This is the noise map the grid and selectons will use
        float[,] layoutNoiseMap = NoiseGenerator.GenerateNoiseMap(layoutMapWidth, layoutMapHeight, layoutMapNoiseData);

        int noiseMapWidth = layoutMapWidth * featuresPerLayoutPerAxis;
        int noiseMapHeight = layoutMapHeight * featuresPerLayoutPerAxis;

        // Then generate a larger scale noise map, and overlay it on the small one
        float[,] featuresNoiseMap = NoiseGenerator.GenerateNoiseMap(noiseMapWidth, noiseMapHeight, featuresMapNoiseData);

        TerrainType[,] terrainMap = PlateauMap(layoutNoiseMap);
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
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, featuresPerLayoutPerAxis), TextureGenerator.TextureFromColorMap(CreateColorMapWithTerrain(noiseMap, terrainMap), noiseMapWidth, noiseMapHeight));

                display.DrawMeshes(MeshGenerator.GenerateTerrainMesh(layoutNoiseMap, 1), TextureGenerator.TextureFromColorMap(CreateColorMap(layoutNoiseMap), layoutNoiseMap.GetLength(0), layoutNoiseMap.GetLength(1)),
                MeshGenerator.GenerateTerrainMesh(featuresNoiseMap, 1), TextureGenerator.TextureFromColorMap(CreateColorMap(featuresNoiseMap), featuresNoiseMap.GetLength(0), featuresNoiseMap.GetLength(1))
                    );

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


    // Test Variables, To remove
    //private int testXSelection = 1;
    //private int testYSelection = 1;

    private Color[] CreateColorMapWithTerrain(float[,] noiseMap, TerrainType[,] terrainTypeMap) {

        int noiseMapWidth = noiseMap.GetLength(0);
        int noiseMapHeight = noiseMap.GetLength(1);

        int terrainMapWidth = terrainTypeMap.GetLength(0);
        int terrainMapHeight = terrainTypeMap.GetLength(1);

        Color[] colorMap = new Color[noiseMapWidth * noiseMapHeight];
        for(int y = 0; y < noiseMapHeight; y++) {
            for(int x = 0; x < noiseMapWidth; x++) {

                if((x + 1) % featuresPerLayoutPerAxis == 0 || (y + 1) % featuresPerLayoutPerAxis == 0) {
                    colorMap[y * noiseMapWidth + x] = Color.black;
                    continue;
                }

                int sampleX = x / (noiseMapWidth / terrainMapWidth);
                int sampleY = y / (noiseMapHeight / terrainMapHeight);

                colorMap[y * noiseMapWidth + x] = terrainTypeMap[sampleX, sampleY].color;

                if(x / featuresPerLayoutPerAxis >= selectedPoint.virtualX && x / featuresPerLayoutPerAxis < selectedPoint.virtualX + 1 &&
                    y / featuresPerLayoutPerAxis >= selectedPoint.virtualY && y / featuresPerLayoutPerAxis < selectedPoint.virtualY + 1) {
                    colorMap[y * noiseMapWidth + x] = colorMap[y * noiseMapWidth + x] + Color.cyan;
                }
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
