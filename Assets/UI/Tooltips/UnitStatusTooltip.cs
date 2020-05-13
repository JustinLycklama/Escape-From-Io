using UnityEngine;
using UnityEngine.UI;

public class UnitStatusTooltip : MonoBehaviour, TrackingUIInterface { //TaskStatusUpdateDelegate
    
    [SerializeField]
    private MasterAndGameTaskCell masterAndGameTaskCell = null;

    [SerializeField]
    private PercentageBar durationBar = null;
    [SerializeField]
    private PercentageBar healthBar = null;
    [SerializeField]
    public PercentageBar percentageBar;

    [SerializeField]
    private UnitTypeIcon unitIcon = null;

    private Unit.FactionType faction;

    private RectTransform targetCanvas;
    //private RectTransform rectTransform;

    // TrackingUIInterface
    public Transform followPosition { get; set; }
    public Transform followingObject { get; set; }

    [SerializeField]
    private CanvasGroup canvas = null;
    public CanvasGroup canvasGroup { get => canvas; }

    //private Unit unit;

    private void Start() {
        durationBar.setDetailTextHidden(true);
        healthBar.setDetailTextHidden(true);

        masterAndGameTaskCell.SetBlackAndWhite();
    }

    private void Update() {
        this.UpdateTrackingPosition();
    }

    public void SetPrimaryActionAndFaction(MasterGameTask.ActionType actionType, Unit.FactionType faction) {
        this.faction = faction;

        unitIcon.SetActionType(actionType, faction);
    }

    public void SetTask(Unit unit, MasterGameTask masterGameTask, GameTask gameTask) {
        //if (task == null) {
        //    taskDescription.text = "Idle";
        //} else {
        //    taskDescription.text = task.description;
        //}

        masterAndGameTaskCell.SetTask(masterGameTask, gameTask);

        //Unit.UnitState unitState = unit.GetUnitState();

        // Override color to display RED on inefficient units
        //Color efficiencyColor = Color.red;
        //if (unitState != Unit.UnitState.Inefficient) {
        Color efficiencyColor = unit.GetUnitState().ColorForState();
        //}

        if (faction == Unit.FactionType.Enemy) {
            efficiencyColor = ColorSingleton.sharedInstance.enemyTaskColor;
        }

        //Color efficiencyColor = new Color(statusColorColor.r * 2, statusColorColor.g * 2, statusColorColor.b * 2);

        //unitInfoCanvas.alpha = unitState == Unit.UnitState.Idle ? 0.7f : 1;        
        masterAndGameTaskCell.Colorize(efficiencyColor);
    }

    public void DisplayPercentageBar(bool display) {
        percentageBar.gameObject.SetActive(display);
    }

    public void SetRemainingDuration(int duration, float percent) {
        durationBar.fillColorImage.color = ColorSingleton.sharedInstance.DurationColorByPercent(percent);
        durationBar.SetPercent(percent, duration.ToString());
    }

    public void SetRemainingHealth(int health, float percent) {
        healthBar.fillColorImage.color = ColorSingleton.sharedInstance.HealthColorByPercent(percent);
        healthBar.SetPercent(percent, health.ToString());
    }
}
