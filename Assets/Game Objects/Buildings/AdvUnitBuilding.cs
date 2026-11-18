using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvUnitBuilding : Building {
    public override string title => "Advanced Unit Building";
    public override float constructionModifierSpeed => 0.20f;  

    protected override void UpdateCompletionPercent(float percent) {

    }

    protected override void CompleteBuilding() {
    }

}
