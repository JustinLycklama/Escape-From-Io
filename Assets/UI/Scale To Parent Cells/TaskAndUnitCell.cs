using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UCharts;

public class TaskAndUnitCell : MonoBehaviour, IPointerClickHandler, TaskQueueDelegate, UnitManagerDelegate, GameButtonDelegate, TimeUpdateDelegate {

    public Text title;
    public Text tasksTile;

    //public Text unitCountText;
    //public Text unitStatusText;

    public PieChart pieChart;

    public Toggle taskListLocked;
    public Text taskListLockedText;

    public Image mainPairConnection;
    public Image[] sidePairConnection;

    public MasterGameTask.ActionType actionType;
    //public Image unitBackgroundSprite;
    public Image taskBackgroundSprite;

    public List<PercentageBar> percentBars;
    private List<Unit> soonToExpireUnits = new List<Unit>();

    private Unit[] unitList = new Unit[0];
    private MasterGameTask[] taskList = new MasterGameTask[0];
    private Unit.UnitState unitListState = Unit.UnitState.Idle;

    private void Start() {
        title.text = actionType.ToString() + "Units";
        tasksTile.text = actionType.ToString() + "Tasks";

        taskListLocked.buttonDelegate = this;

        Script.Get<TaskQueueManager>().RegisterForNotifications(this, actionType);
        Script.Get<UnitManager>().RegisterForNotifications(this, actionType);
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);

        for(int i = 0; i < percentBars.Count; i++) {
            PercentageBar bar = percentBars[i];

            bar.setDetailTextHidden(true);
        }


        var colors = new List<Color32> {
            Unit.UnitState.Idle.ColorForState(), Unit.UnitState.Efficient.ColorForState(), Unit.UnitState.Inefficient.ColorForState()
        };

        pieChart.SetColors(colors);

        var data = new List<PieChartDataNode> {
            new PieChartDataNode("", 1), new PieChartDataNode("", 1), new PieChartDataNode("", 1)
        };

        pieChart.SetData(data);

        SecondUpdated();
    }

    private void OnDestroy() {
        try {
            Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
            Script.Get<UnitManager>().EndNotifications(this, actionType);
            Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
        } catch(System.NullReferenceException e) { }
    }


    private void UpdateCellColor() {
        //unitBackgroundSprite.color = unitListState.ColorForState();                
    }

    private void SetLockState(bool state) {
        taskListLocked.SetState(state);

        Unit.UnitState taskListColorState = state ? Unit.UnitState.Efficient : Unit.UnitState.Inefficient;
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

        SetLockAndCount();
    }

    private void SetLockAndCount() {

        string stateText = taskListLocked.state ? "Paired" : "Open";
        string count = taskList.Length.ToString();

        taskListLockedText.text = $"{count} {stateText}";
    }


    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if (button == taskListLocked) {
            SetLockState(!taskListLocked.state);

            Script.Get<TaskQueueManager>().SetTaskListLocked(actionType, taskListLocked.state);
        }
    }

    /*
     * IPointerClickHandler Interface
     * */

    public void OnPointerClick(PointerEventData eventData) {
        TaskAndUnitDetailPanel detailPanel = Script.Get<UIManager>().Push(UIManager.Blueprint.TaskAndUnitDetail) as TaskAndUnitDetailPanel;
        detailPanel.SetActionType(actionType);
    }

    /*
     * TaskQueueDelegate Interface
     * */

    public void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType, TaskQueueManager.ListState listState) {
        this.taskList = taskList;        
        SetLockAndCount();

        SetLockState(Script.Get<TaskQueueManager>().GetTaskListLockStatus(actionType));

        UpdateCellColor();
    }

    /*
     * UnitManagerDelegate Interface
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType, Unit.UnitState unitListState) {
        this.unitList = unitList;
        this.unitListState = unitListState;

        //unitCountText.text = unitList.Length + " Unit" + ((unitList.Length == 1) ? "" : "s");
        //unitStatusText.text = unitListState.decription();

        UpdateCellColor();

        soonToExpireUnits = unitList.OrderBy(unit => unit.remainingDuration).Take(percentBars.Count).ToList();        
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {

        DateTime now = DateTime.Now;

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

