using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipReactor : Building {
    public override string title => "Reactor";
    protected override float constructionModifierSpeed => 0.20f;

    protected override void Awake() {
        base.Awake();
        ResearchSingleton.sharedInstance.AddBuildingCount(this);
    }

    public override void Destroy() {
        base.Destroy();
        ResearchSingleton.sharedInstance.RemoveBuildingCount(this);
    }

    protected override void UpdateCompletionPercent(float percent) {

    }

    protected override void CompleteBuilding() {
        ResearchSingleton.sharedInstance.unitDurationAddition = 60;

        Unit[] allUnits = Script.Get<UnitManager>().GetAllUnits();
        foreach(Unit unit in allUnits) {
            unit.remainingDuration += ResearchSingleton.sharedInstance.unitDurationAddition;
        }

        Script.Get<NotificationPanel>().AddNotification(new NotificationItem("Ship Reactor Complete! Units last " + ResearchSingleton.sharedInstance.unitDurationAddition + "s longer!", transform));
    }
}
