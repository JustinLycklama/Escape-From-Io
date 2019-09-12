using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBuilding : Building
{
    public Unit associatedUnit;

    protected override string title => associatedUnit.title;
    public override string description => associatedUnit.title;

    protected override float constructionModifierSpeed => 0.35f;

    protected override void CompleteBuilding() {
        associatedUnit.Initialize();
        Script.Get<BuildingManager>().RemoveBuilding(this);
    }

    protected override void UpdateCompletionPercent(float percent) {
       
    }    
}
