using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostPanelTooltip : CostPanel, TrackingUIInterface {
    public Transform toFollow { get; set; }
    public CanvasGroup canvas;
    public CanvasGroup canvasGroup { get => canvas; }

    private void Update() {
        this.UpdateTrackingPosition();
    }

    public void MoveUnusedCostToResourceManager() {
        Script.Get<GameResourceManager>().CostPanelToEnvironmentDump(tallyCountDictionary);
    }
}