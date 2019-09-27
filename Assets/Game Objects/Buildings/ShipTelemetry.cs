using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipTelemetry : Building {
    public override string title => "Telemetry";
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
        ResearchSingleton.sharedInstance.visionRadiusAddiiton = 2;
        Script.Get<BuildingManager>().RecalcluateSightStatuses();

        Script.Get<NotificationPanel>().AddNotification(new NotificationItem("Ship Telemetry Complete! Tower sight increased from 3 to 5!", transform));
    }

}
