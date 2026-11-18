using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : Building {

    public override string title => "Tower";
    public override float constructionModifierSpeed => 0.20f;

    public override BuildingEffectStatus BuildingStatusEffects() {
        return BuildingEffectStatus.Light;
    }

    public override int BuildingStatusRange() {
        return 3 + ResearchSingleton.sharedInstance.visionRadiusAddiiton;
    }

    protected override void UpdateCompletionPercent(float percent) {

    }

    protected override void CompleteBuilding() {

    }
}
