using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBuilding : Building
{
    public Unit associatedUnit;

    public override string title => associatedUnit.title;
    public override string description => associatedUnit.title;

    public override float constructionModifierSpeed  => 0.15f;

    protected override void CompleteBuilding() {
        associatedUnit.Initialize();
        Script.Get<BuildingManager>().RemoveBuilding(this);
    }

    protected override void UpdateCompletionPercent(float percent) {
       
    }

    public override void Destroy() {
        associatedUnit.DestroySelf();
        base.Destroy();        
    }
}
