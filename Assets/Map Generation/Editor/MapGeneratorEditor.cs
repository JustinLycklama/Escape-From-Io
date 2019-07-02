using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {
    public override void OnInspectorGUI() {

        MapGenerator mapGen = (MapGenerator)target;

        if(DrawDefaultInspector()) {
            Generate(mapGen);
        }

        if(GUILayout.Button("Generate")) {
            Generate(mapGen);
        }
    }

    private void Generate(MapGenerator mapGen) {
        /*Constants constants = Script.Get<Constants>();

        int mapWidth = constants.layoutMapWidth;
        int mapHeight = constants.layoutMapHeight;

        float[,] layoutNoiseMap = mapGen.GenerateLayoutMap(mapWidth, mapHeight);
        TerrainType[,] terrainMap = mapGen.PlateauMap(layoutNoiseMap);

        float[,] groundMutatorMap = mapGen.GenerateGroundMutatorMap(mapWidth, mapHeight);
        float[,] mountainMutatorMap = mapGen.GenerateMountainMutatorMap(mapWidth, mapHeight);

        TerrainManager terrainManager = Script.Get<TerrainManager>();
        terrainManager.SetGroundMutatorMap(groundMutatorMap);
        terrainManager.SetMounainMutatorMap(mountainMutatorMap);

        float[,] groundFeaturesNoiseMap = mapGen.GenerateGroundFeaturesMap(mapWidth * constants.featuresPerLayoutPerAxis, mapHeight * constants.featuresPerLayoutPerAxis);
        float[,] mountainFeaturesNoiseMap = mapGen.GenerateMountainFeaturesMap(mapWidth * constants.featuresPerLayoutPerAxis, mapHeight * constants.featuresPerLayoutPerAxis);

        mapGen.GenerateMap(mapGen.demoMapContainer, terrainMap, layoutNoiseMap, groundFeaturesNoiseMap, mountainFeaturesNoiseMap);
        mapGen.demoMapContainer.SetupMaterialShader();*/
    }
}
