using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipThrusters : Building {
    public static int count = 0;

    public override string title => "Thrusters";
    protected override float constructionModifierSpeed => 0.20f;

    protected override void Awake() {
        base.Awake();
        count++;
    }

    public override void Destroy() {
        base.Destroy();
        count--;
    }

    protected override void UpdateCompletionPercent(float percent) {

    }

    protected override void CompleteBuilding() {
        ResearchSingleton.sharedInstance.unitSpeedMultiplier = 1.333f;
    }
}
