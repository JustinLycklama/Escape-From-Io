using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Refinery : Building {

    public override string title => "Refinery";
    protected override float constructionModifierSpeed => 0.35f;

    //protected override int requiredOre => 5;
    protected override void UpdateCompletionPercent(float percent) {
        throw new System.NotImplementedException();
    }

    protected override void CompleteBuilding() {
        throw new System.NotImplementedException();
    }
}
