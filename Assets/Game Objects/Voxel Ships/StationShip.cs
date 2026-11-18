using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationShip : Building, CanSceneChangeDelegate /*, BuildingsUpdateDelegate*/ {

    private bool canSceneChange = false;

    public override string title => "Starship Frame";
    public override float constructionModifierSpeed => 0.15f;

    private void OnDestroy() {
    }

    protected override void CompleteBuilding() {
        Script.Get<Narrator>().EndGameSuccess();


    }

    protected override void UpdateCompletionPercent(float percent) {
        
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return canSceneChange;
    }

    /*
     * BuildingsUpdateDelegate Interface
     * */

}
