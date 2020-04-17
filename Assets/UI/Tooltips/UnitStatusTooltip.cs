using UnityEngine;
using UnityEngine.UI;

public class UnitStatusTooltip : MonoBehaviour, TrackingUIInterface { //TaskStatusUpdateDelegate

    public Text title;
    public Text taskDescription;
    //public Image taskEfficiencyImage;

    public CanvasGroup unitInfoCanvas;

    public PercentageBar durationBar;
    public PercentageBar percentageBar;
    public PercentageBar healthBar;

    public Image backgroundSprite;

    private RectTransform targetCanvas;
    //private RectTransform rectTransform;

    // TrackingUIInterface
    public Transform toFollow { get; set; }
    public CanvasGroup canvas;
    public CanvasGroup canvasGroup { get => canvas; }

    //private Unit unit;

    private void Start() {
        durationBar.setDetailTextHidden(true);
        healthBar.setDetailTextHidden(true);
    }

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

        Unit.UnitState unitState = unit.GetUnitState();

        // Override color to display RED on inefficient units
        Color efficiencyColor = Color.red;
        if (unitState != Unit.UnitState.Inefficient) {
            efficiencyColor = unit.GetUnitState().ColorForState();
        }

        unitInfoCanvas.alpha = unitState == Unit.UnitState.Idle ? 0.7f : 1;        
        backgroundSprite.color = efficiencyColor;
    }

    public void DisplayPercentageBar(bool display) {
        percentageBar.gameObject.SetActive(display);
    }

    public void SetRemainingDuration(int duration, float percent) {
        durationBar.fillColorImage.color = ColorSingleton.sharedInstance.GreenToRedByPercent(percent);
        durationBar.SetPercent(percent, duration.ToString());
    }

    public void SetRemainingHealth(int health, float percent) {
        healthBar.fillColorImage.color = ColorSingleton.sharedInstance.GreenToRedByPercent(percent);
        healthBar.SetPercent(percent, health.ToString());
    }
}
