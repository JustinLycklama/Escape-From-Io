using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostPanelTooltip : CostPanel, TrackingUIInterface {
    public Transform toFollow { get; set; }

    private void Update() {
        this.UpdateTrackingPosition();
    }
}