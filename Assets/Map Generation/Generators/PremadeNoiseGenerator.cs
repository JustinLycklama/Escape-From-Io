using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using R = RegionType.Type;
using T = TerrainType.Type;

public class PremadeNoiseGenerator : MonoBehaviour
{
    [SerializeField]
    //private float[,] layoutNoiseData;
    public float[,] LayoutNoiseData;// {
                                    //    get {
                                    //        if (layoutNoiseData == null) {
                                    //            layoutNoiseData = new float[10, 10];


    //        }

    //        var tileTypes = TwoByFiveTileType();


    //        return query.s
    //    }
    //}

    /*
     * [HideInInspector]
public Dictionary<RegionType.Type, RegionType> regionTypeMap;

[HideInInspector]
public Dictionary<RegionType, List<TerrainType>> regionToTerrainTypeMap;

[HideInInspector]
public Dictionary<TerrainType.Type, TerrainType> terrainTypeMap;
     * */

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

            //LayoutNoiseData = ThreeAndFiveLayout();
            //GroundMutatorNoiseData = ThreeAndFiveGroundMutator();
            //MountainMutatorNoiseData = ThreeAndFiveMountainMutator();
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
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Land, T.Empty), (R.Land, T.Empty), (R.Land, T.Water), (R.Mountain, T.SolidRock), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) },
        { (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water),  (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water), (R.Water, T.Water) } };
    }

    //private float[,] ThreeAndFiveLayout() {
    //    return new float[15,15] { 
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.50f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.99f}};
    //}

    //private float[,] ThreeAndFiveGroundMutator() {
    //    return new float[15, 15] {
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.99f}};
    //}

    //private float[,] ThreeAndFiveMountainMutator() {
    //    return new float[15, 15] {
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.35f,  0.35f,  0.35f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f},
    //    { 0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.01f,  0.99f}};
    //}
}
