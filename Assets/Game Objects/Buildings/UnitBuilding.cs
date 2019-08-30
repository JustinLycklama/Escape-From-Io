using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBuilding : Building
{
    public Unit associatedUnit;
    public override string description => associatedUnit.title;

    protected override void CompleteBuilding() {
        associatedUnit.Initialize();
    }

    protected override void UpdateCompletionPercent(float percent) {
       
    }    
}
