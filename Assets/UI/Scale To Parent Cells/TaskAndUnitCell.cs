using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UCharts;

public class TaskAndUnitCell : MonoBehaviour, IPointerClickHandler, TaskQueueDelegate, UnitManagerDelegate, GameButtonDelegate, TimeUpdateDelegate {

    public enum TaskAndUnitCellTutorialIdentifier {
        Details,
        LockToggle
    }

    [Serializable]
    private struct TaskIconFlow {
        public MasterGameTask.ActionType actionType;

        public Image flowImage;
        public UnitTypeIcon typeIcon;        
    }


    [SerializeField]
    private MasterGameTask.ActionType actionType;

    [SerializeField]
    private Text iconTitle;
    [SerializeField]
    private UnitTypeIcon unitIconImage;

    [SerializeField]
    private GameButton unitListButton;

    [SerializeField]
    private Text unitsTitle;
    [SerializeField]
    private Text unitsCountText;
    [SerializeField]
    private List<PercentageBar> percentBars;
    [SerializeField]
    private PieChart pieChart;

    [SerializeField]
    private Text tasksTile;
    [SerializeField]
    private Text taskCountText;
    [SerializeField]
    private List<TaskIconFlow> taskIconFlowList;

    [SerializeField]
    private GameButton taskListLockButton;
    [SerializeField]
    private Toggle taskListLockToggle;

    [SerializeField]
    private Image taskListLockBackground;

    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private Color disabledColor;


    [SerializeField]
    private Sprite openIcon;
    [SerializeField]
    private Sprite lockedIcon;

    //public Toggle taskListLocked;
    //public Text taskListLockedText;

    //public Image mainPairConnection;
    //public Image[] sidePairConnection;


    //public Image unitBackgroundSprite;
    //public Image taskBackgroundSprite;

    private List<Unit> soonToExpireUnits = new List<Unit>();

    private Unit[] unitList = new Unit[0];
    private MasterGameTask[] taskList = new MasterGameTask[0];

    //private Unit.UnitState unitListState = Unit.UnitState.Idle;

    private void Start() {
        iconTitle.text = actionType.ToString();
        unitsTitle.text = actionType.ToString() + " Bots";
        tasksTile.text = actionType.ToString() + " Tasks";

        unitIconImage.SetActionType(actionType);

        unitListButton.buttonDelegate = this;
        taskListLockButton.buttonDelegate = this;
        taskListLockToggle.buttonDelegate = this;

        Script.Get<TaskQueueManager>().RegisterForNotifications(this, actionType);
        Script.Get<UnitManager>().RegisterForNotifications(this, actionType);
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);

        for(int i = 0; i < percentBars.Count; i++) {
            PercentageBar bar = percentBars[i];

            bar.setDetailTextHidden(true);
        }


        var colors = new List<Color32> {
            ColorForState(Unit.UnitState.Idle), ColorForState(Unit.UnitState.Inefficient), ColorForState(Unit.UnitState.Efficient)
        };

        pieChart.SetColors(colors);

        SecondUpdated();
    }

    private void OnDestroy() {
        try {
            Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
            Script.Get<UnitManager>().EndNotifications(this, actionType);
            Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
        } catch(System.NullReferenceException e) { }
    }


    private void UpdatePieChart(Unit[] unitList) {
        //unitBackgroundSprite.color = unitListState.ColorForState();     

        List<Unit.UnitState> unitStates = unitList.Select(unit => unit.GetUnitState()).ToList();
        var idleNode = new PieChartDataNode("", unitStates.Where(state => state == Unit.UnitState.Idle).Count());
        var inefficientNode = new PieChartDataNode("", unitStates.Where(state => state == Unit.UnitState.Inefficient).Count());
        var efficientNode = new PieChartDataNode("", unitStates.Where(state => state == Unit.UnitState.Efficient).Count());

        pieChart.SetData(new List<PieChartDataNode> {
            idleNode, inefficientNode, efficientNode
        });

        unitsCountText.text = unitList.Length.ToString();
    }

    private void SetLockState(bool state) {
        //taskListLocked.SetState(state);

        //Unit.UnitState taskListColorState = state ? Unit.UnitState.Efficient : Unit.UnitState.Inefficient;
        //taskBackgroundSprite.color = taskListColorState.ColorForState();

        //Color mainPairConnectionColor = mainPairConnection.color;
        //Color sidePairConnectionColor = mainPairConnectionColor;

        //if (state == true) {
        //    mainPairConnectionColor.a = 1f;
        //    sidePairConnectionColor.a = 0.1f;
        //} else {
        //    mainPairConnectionColor.a = 0.5f;
        //    sidePairConnectionColor.a = 0.8f;
        //}

        //mainPairConnection.color = mainPairConnectionColor;

        //foreach(Image image in sidePairConnection) {
        //    image.color = sidePairConnectionColor;
        //}        

        foreach(TaskIconFlow flow in taskIconFlowList.Where(flow => flow.actionType != actionType)) {
            flow.flowImage.sprite = !state ? openIcon : lockedIcon;
            flow.flowImage.color =  new Color(flow.flowImage.color.r, flow.flowImage.color.g, flow.flowImage.color.b, state ? 0.45f : 1.0f);
            flow.typeIcon.SetEnabled(!state);
        }

        taskListLockBackground.color = state ? defaultColor : disabledColor;
        taskListLockToggle.SetState(state);

        UpdateLockAndCountUI();
    }

    private void UpdateLockAndCountUI() {
        //string stateText = taskListLocked.state ? "Paired" : "Open";
        //string count = taskList.Length.ToString();

        //taskListLockedText.text = $"{count} {stateText}";
        taskCountText.text = taskList.Select(task => {
            if (task.repeatCount > 0) {
                return task.repeatCount;
            }

            return 1;
        }).Sum().ToString();
    }       

    public Color ColorForState(Unit.UnitState unitState) {

        switch(unitState) {
            case Unit.UnitState.Idle:
                return ColorSingleton.sharedInstance.idleUnitColor;
            case Unit.UnitState.Efficient:
                return ColorSingleton.sharedInstance.efficientColor;
            case Unit.UnitState.Inefficient:
                return ColorSingleton.sharedInstance.inefficientUnitColor;
        }

        return Color.white;
    }

    private void PushDetailPanel() {
        TaskAndUnitDetailPanel detailPanel = Script.Get<UIManager>().Push(UIManager.Blueprint.TaskAndUnitDetail) as TaskAndUnitDetailPanel;
        detailPanel.SetActionType(actionType);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {

        var currentTutorialIdentifier = TutorialManager.isolateUserAction;

        if (button == unitListButton) {

            // Skip if the tutorial is forcing us elsewhere
            if(currentTutorialIdentifier != null &&
                (currentTutorialIdentifier.actionType != actionType || currentTutorialIdentifier.cellIdentifier != TaskAndUnitCellTutorialIdentifier.Details)) {
                return;
            }

            PushDetailPanel();
            TutorialManager.sharedInstance.Fire(TutorialTrigger.TaskAndUnitDetails);
        } else if(button == taskListLockButton || button == taskListLockToggle) {

            // Skip if the tutorial is forcing us elsewhere
            if(currentTutorialIdentifier != null &&
                (currentTutorialIdentifier.actionType != actionType || currentTutorialIdentifier.cellIdentifier != TaskAndUnitCellTutorialIdentifier.LockToggle)) {
                return;
            }

            TaskQueueManager taskQueueManager = Script.Get<TaskQueueManager>();
            var newState = !taskQueueManager.GetTaskListLockStatus(actionType);

            SetLockState(newState);
            taskQueueManager.SetTaskListLocked(actionType, newState);
        }
    }

    /*
     * IPointerClickHandler Interface
     * */

    public void OnPointerClick(PointerEventData eventData) {
        ButtonDidClick(unitListButton);
    }

    /*
     * TaskQueueDelegate Interface
     * */

    public void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType, TaskQueueManager.ListState listState) {
        this.taskList = taskList;        
        UpdateLockAndCountUI();

        SetLockState(Script.Get<TaskQueueManager>().GetTaskListLockStatus(actionType));

        //UpdatePieChart();
    }

    /*
     * UnitManagerDelegate Interface
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType) {
        this.unitList = unitList;

        //this.unitListState = unitListState;

        //unitCountText.text = unitList.Length + " Unit" + ((unitList.Length == 1) ? "" : "s");
        //unitStatusText.text = unitListState.decription();

        UpdatePieChart(unitList);

        soonToExpireUnits = unitList.OrderBy(unit => unit.remainingDuration).Take(percentBars.Count).ToList();        
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {

        //DateTime now = DateTime.Now;

        for(int i = 0; i < percentBars.Count; i++) {
            PercentageBar bar = percentBars[i];

            bool activate = soonToExpireUnits.Count > i;
            if (bar.gameObject.activeSelf != activate) {
                bar.gameObject.SetActive(activate);
            }

            if (soonToExpireUnits.Count > i) {
                int remainingDuration = soonToExpireUnits[i].remainingDuration;
                float percentComplete = (float) remainingDuration / (float)Unit.maxUnitUduration;

                bar.SetPercent(percentComplete);
                bar.fillColorImage.color = ColorSingleton.sharedInstance.DurationColorByPercent(percentComplete);
            }
        }
    }
}

