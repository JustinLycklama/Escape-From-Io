using UnityEngine;
using UnityEngine.UI;

public class UnitStatusTooltip : MonoBehaviour, TrackingUIInterface { //TaskStatusUpdateDelegate

    private static Color startDurationColor = new Color(0, 1, 0.572549f);
    private static Color endDurationColor = new Color(1, 0.08551968f, 0);

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
        durationBar.fillColorImage.color = Color.Lerp(endDurationColor, startDurationColor, 1 - percent);
        durationBar.SetPercent(1 - percent, duration.ToString());
    }
}
