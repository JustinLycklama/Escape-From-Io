using UnityEngine;
using UnityEngine.UI;

public class UnitStatusTooltip : MonoBehaviour, TrackingUIInterface { //TaskStatusUpdateDelegate

    public Text title;
    public Text taskDescription;
    //public Image taskEfficiencyImage;

    public PercentageBar durationBar;
    public PercentageBar percentageBar;

    public Image backgroundSprite;

    private RectTransform targetCanvas;
    private RectTransform rectTransform;

    // TrackingUIInterface
    public Transform toFollow { get; set; }

    //private Unit unit;

    private void Update() {
        this.UpdateTrackingPosition();
    }

    public void SetTitle(string title) {
        this.title.text = title;
    }

    public void SetTask(Unit unit, GameTask task) {
        if (task == null) {
            taskDescription.text = "Idle";
        } else {
            taskDescription.text = task.description;
        }

        backgroundSprite.color = unit.GetUnitState().ColorForState();
    }

    public void DisplayPercentageBar(bool display) {
        percentageBar.gameObject.SetActive(display);
    }

    public void SetRemainingDuration(int duration, float percent) {
        durationBar.fillColorImage.color = ColorSingleton.sharedInstance.GreenToRedByPercent(percent);
        durationBar.SetPercent(percent, duration.ToString());
    }
}
