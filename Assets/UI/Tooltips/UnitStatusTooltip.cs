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
    private MasterGameTask.ActionType primaryActionType;

    // #21: markers stay hidden unless hovered (TrackingUIInterface), selected, or the robot needs attention
    [HideInInspector]
    public Unit owner;
    private bool selected;
    private bool lowBattery;
    private float idleWithWorkSince = -1;
    private const float idleAttentionDelay = 3f;
    private TaskQueueManager taskQueueManager;

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
        this.UpdateTrackingPosition(selected || NeedsAttention() ? 1f : 0f);
    }

    public void SetPrimaryActionAndFaction(MasterGameTask.ActionType actionType, Unit.FactionType faction) {
        this.faction = faction;
        this.primaryActionType = actionType;

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

    public void SetSelected(bool selected) {
        this.selected = selected;
    }

    // Low battery, or idle for a few seconds while its own queue has work (unreachable or refused tasks)
    private bool NeedsAttention() {
        if (faction != Unit.FactionType.Player || owner == null) {
            return false;
        }

        if (lowBattery) {
            return true;
        }

        if (taskQueueManager == null) {
            taskQueueManager = Script.Get<TaskQueueManager>();
        }

        bool idleWithWork = owner.GetUnitState() == Unit.UnitState.Idle && taskQueueManager != null && taskQueueManager.QueuedTaskCount(primaryActionType) > 0;
        if (!idleWithWork) {
            idleWithWorkSince = -1;
            return false;
        }

        if (idleWithWorkSince < 0) {
            idleWithWorkSince = Time.time;
        }

        return Time.time - idleWithWorkSince > idleAttentionDelay;
    }

    public void DisplayPercentageBar(bool display) {
        percentageBar.gameObject.SetActive(display);
    }

    public void SetRemainingDuration(int duration, float percent) {
        lowBattery = duration <= NotificationPanel.unitDurationWarning;
        durationBar.fillColorImage.color = ColorSingleton.sharedInstance.DurationColorByPercent(percent);
        durationBar.SetPercent(percent, duration.ToString());
    }

    public void SetRemainingHealth(int health, float percent) {
        healthBar.fillColorImage.color = ColorSingleton.sharedInstance.HealthColorByPercent(percent);
        healthBar.SetPercent(percent, health.ToString());
    }
}
