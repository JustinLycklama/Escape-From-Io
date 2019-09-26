using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipProps : Building {

    public override string title => "Starship Frame";
    protected override float constructionModifierSpeed => 0.25f;

    protected override void UpdateCompletionPercent(float percent) {
    }

    protected override void CompleteBuilding() {
    }
}
