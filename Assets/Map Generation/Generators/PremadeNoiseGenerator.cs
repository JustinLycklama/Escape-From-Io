using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using R = RegionType.Type;
using T = TerrainType.Type;

public class PremadeNoiseGenerator : MonoBehaviour {

    public float[,] LayoutNoiseData = null;
    public float[,] GroundMutatorNoiseData = null;
    public float[,] MountainMutatorNoiseData = null;

    private void Start() {
        Constants constants = Script.Get<Constants>();
        TerrainManager terrainManager = Script.Get<TerrainManager>();

        if(constants.layoutMapHeight == 5 && constants.layoutMapWidth == 5 &&
            constants.mapCountX == 2 && constants.mapCountY == 2) {

            var tileTypes = TwoByFiveTileType();

            LayoutNoiseData = new float[10, 10];
            GroundMutatorNoiseData = new float[10, 10];
            MountainMutatorNoiseData = new float[10, 10];

            for(int x = 0; x < 10; x++) {
                for(int y = 0; y < 10; y++) {
                    R regionType = tileTypes[x, y].Item1;
                    T terrainType = tileTypes[x, y].Item2;

                    RegionType regionObject = terrainManager.regionTypeMap[regionType];
                    TerrainType terrainObject = terrainManager.terrainTypeMap[terrainType];


                    LayoutNoiseData[x, y] = (regionObject.noiseBase + regionObject.noiseMax) / 2.0f;
                    GroundMutatorNoiseData[x, y] = (terrainObject.mutatorNoiseBase + terrainObject.mutatorNoiseMax) / 2.0f;
                    MountainMutatorNoiseData[x, y] = GroundMutatorNoiseData[x, y];
                }
            }
        }
    }

    private (RegionType.Type, TerrainType.Type)[,] TwoByFiveTileType() {
        return new (RegionType.Type, TerrainType.Type)[10, 10] {
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Mountain, T.SolidRock), (R.Mountain, T.SolidRock), (R.Mountain, T.SolidRock), (R.Mountain, T.SolidRock), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Land, T.Empty), (R.Land, T.Empty), (R.Land, T.Empty), (R.Mountain, T.SolidRock), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Land, T.Empty), (R.Land, T.Empty), (R.Land, T.Empty), (R.Mountain, T.SolidRock), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Land, T.Empty), (R.Land, T.Empty), (R.Land, T.Empty), (R.Mountain, T.SolidRock), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) } };
    }
}
