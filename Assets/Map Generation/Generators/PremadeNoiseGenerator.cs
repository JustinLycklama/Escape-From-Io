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

    public void SetupCustomMap() {
        Constants constants = Script.Get<Constants>();
        TerrainManager terrainManager = Script.Get<TerrainManager>();

        constants.layoutMapWidth = 5;
        constants.layoutMapHeight = 5;

        constants.mapCountX = 2;
        constants.mapCountY = 2;

        (R, T)[,] tileTypes = null;

        if (TutorialManager.sharedInstance.tutorialType == null) {
            return;
        }

        switch(TutorialManager.sharedInstance.tutorialType.Value) {
            case TutorialType.Basic:
                tileTypes = BasicTutorialLayout();
                break;
            case TutorialType.Defense:
                tileTypes = DefenseTutorialLayout();
                break;
            case TutorialType.Escape:
                tileTypes = EscapeTutorialLayout();
                break;
        }

        LayoutNoiseData = new float[10, 10];
        GroundMutatorNoiseData = new float[10, 10];
        MountainMutatorNoiseData = new float[10, 10];

        for(int x = 0; x < 10; x++) {
            for(int y = 0; y < 10; y++) {
                R regionType = tileTypes[y, x].Item1;
                T terrainType = tileTypes[y, x].Item2;

                RegionType regionObject = terrainManager.regionTypeMap[regionType];
                TerrainType terrainObject = terrainManager.terrainTypeMap[terrainType];


                LayoutNoiseData[x, y] = (regionObject.noiseBase + regionObject.noiseMax) / 2.0f;
                GroundMutatorNoiseData[x, y] = (terrainObject.mutatorNoiseBase + terrainObject.mutatorNoiseMax) / 2.0f;
                MountainMutatorNoiseData[x, y] = GroundMutatorNoiseData[x, y];
            }
        }
    }

    private (R, T)[,] BasicTutorialLayout() {

        var WT = (R.Water, T.Water);
        var EL = (R.Land, T.Empty);
        var SR = (R.Mountain, T.SolidRock);
        var LR = (R.Mountain, T.LooseRock);
        var HR = (R.Mountain, T.HardRock);
        var AL = (R.Mountain, T.AlunarRock);

        return new (R, T)[10, 10] {
        { WT,  WT,  HR,  EL, WT, WT, WT, WT, WT, WT },
        { WT,  EL,  HR,  EL, EL, EL, EL, WT, WT, WT },
        { WT,  EL,  SR,  SR, SR, SR, LR, WT, WT, WT },
        { AL,  AL,  SR,  EL, EL, SR, EL, WT, WT, WT },
        { WT,  AL,  SR,  EL, EL, SR, EL, SR, WT, WT },
        { WT,  WT,  SR,  SR, LR, SR, LR, WT, WT, WT },
        { WT,  WT,  LR,  EL, EL, LR, LR, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, AL, AL, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT } };
    }

    private (R, T)[,] DefenseTutorialLayout() {

        var WT = (R.Water, T.Water);
        var EL = (R.Land, T.Empty);
        var SL = (R.Land, T.Grass);
        var SR = (R.Mountain, T.SolidRock);
        var LR = (R.Mountain, T.LooseRock);
        var HR = (R.Mountain, T.HardRock);

        return new (R, T)[10, 10] {
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, SR, SR, SR },
        { WT,  WT,  WT,  LR, SR, WT, WT, HR, HR, SR },
        { WT,  WT,  LR,  SL, SL, EL, EL, EL, HR, SR },
        { WT,  WT,  SR,  SL, SL, WT, WT, EL, HR, SR },
        { WT,  WT,  WT,  WT, WT, WT, WT, SR, SR, SR },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT } };
    }

    private (R, T)[,] EscapeTutorialLayout() {

        var WT = (R.Water, T.Water);
        var EL = (R.Land, T.Empty);
        var SR = (R.Mountain, T.SolidRock);
        var LR = (R.Mountain, T.LooseRock);
        var HR = (R.Mountain, T.HardRock);
        var AL = (R.Mountain, T.AlunarRock);

        return new (R, T)[10, 10] {
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT },
        { WT,  WT,  WT,  WT, WT, WT, WT, WT, WT, WT } };
    }
}
