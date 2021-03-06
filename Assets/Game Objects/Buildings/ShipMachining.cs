﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipMachining : Building {
    public override string title => "Machining";
    public override float constructionModifierSpeed => 0.20f;

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
        ResearchSingleton.sharedInstance.unitActionMultiplier = 1.5f;

        Script.Get<NotificationPanel>().AddNotification(new NotificationItem("Units Build, Mine and Clean 50% faster!", NotificationType.TaskComplete, transform));
    }
}
