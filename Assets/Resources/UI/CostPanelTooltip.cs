using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CostPanelTooltip : CostPanel, TrackingUIInterface {

    public Transform followPosition { get; set; }
    public Transform followingObject { get; set; }
    public CanvasGroup canvas;
    public CanvasGroup canvasGroup { get => canvas; }

    [SerializeField]
    private Text title = null;

    private void Update() {
        this.UpdateTrackingPosition();
    }

    public void SetTitle(string text) {
        title.text = text;
    }

    public void MoveUnusedCostToResourceManager() {
        Script.Get<GameResourceManager>().CostPanelToEnvironmentDump(tallyCountDictionary);
    }
}