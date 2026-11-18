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

    // TrackingUIInterface
    public Transform followPosition { get; set; }
    public Transform followingObject { get; set; }

    [SerializeField]
    private CanvasGroup canvas = null;
    public CanvasGroup canvasGroup { get => canvas; }

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

        masterAndGameTaskCell.SetTask(masterGameTask, gameTask);

        Color efficiencyColor = unit.GetUnitState().ColorForState();

        if (faction == Unit.FactionType.Enemy) {
            efficiencyColor = ColorSingleton.sharedInstance.enemyTaskColor;
        }

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
