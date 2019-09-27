using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipProps : Building {
    public override string title => "Starship Frame";
    protected override float constructionModifierSpeed => 0.25f;

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
        Script.Get<NotificationPanel>().AddNotification(new NotificationItem("Ship Frame Complete.", transform));
    }
}
