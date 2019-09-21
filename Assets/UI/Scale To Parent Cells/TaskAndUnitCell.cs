using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class TaskAndUnitCell : MonoBehaviour, IPointerClickHandler, TaskQueueDelegate, UnitManagerDelegate, GameButtonDelegate, TimeUpdateDelegate {

    public Text title;

    public Text unitCountText;
    public Text unitStatusText;

    public Text taskCountText;
    public Toggle taskListLocked;
    public Text taskListLockedText;

    public Image mainPairConnection;
    public Image[] sidePairConnection;

    public MasterGameTask.ActionType actionType;
    public Image unitBackgroundSprite;
    public Image taskBackgroundSprite;

    public List<PercentageBar> percentBars;
    private List<Unit> soonToExpireUnits = new List<Unit>();

    private Unit[] unitList = new Unit[0];
    private MasterGameTask[] taskList = new MasterGameTask[0];
    private Unit.UnitState unitListState = Unit.UnitState.Idle;

    private void Start() {
        title.text = actionType.ToString();

        taskListLocked.buttonDelegate = this;

        Script.Get<TaskQueueManager>().RegisterForNotifications(this, actionType);
        Script.Get<UnitManager>().RegisterForNotifications(this, actionType);
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);

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
        unitBackgroundSprite.color = unitListState.ColorForState();                
    }

    private void SetLockState(bool state) {
        taskListLocked.SetState(state);

        Unit.UnitState taskListColorState = state ? Unit.UnitState.Efficient : Unit.UnitState.Inefficient;
        taskBackgroundSprite.color = taskListColorState.ColorForState();

        Color mainPairConnectionColor = mainPairConnection.color;
        Color sidePairConnectionColor = mainPairConnectionColor;

        if (state == true) {
            mainPairConnectionColor.a = 1f;
            sidePairConnectionColor.a = 0.1f;
        } else {
            mainPairConnectionColor.a = 0.5f;
            sidePairConnectionColor.a = 0.8f;
        }

        mainPairConnection.color = mainPairConnectionColor;

        foreach(Image image in sidePairConnection) {
            image.color = sidePairConnectionColor;
        }

        taskListLockedText.text = state ? "Paired" : "Open";
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
        taskCountText.text = taskList.Length.ToString(); // + " Task" + ((taskList.Length == 1)? "" : "s");

        SetLockState(Script.Get<TaskQueueManager>().GetTaskListLockStatus(actionType));

        UpdateCellColor();
    }

    /*
     * UnitManagerDelegate Interface
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType, Unit.UnitState unitListState) {
        this.unitList = unitList;
        this.unitListState = unitListState;

        unitCountText.text = unitList.Length + " Unit" + ((unitList.Length == 1) ? "" : "s");
        unitStatusText.text = unitListState.decription();

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
                bar.fillColorImage.color = ColorSingleton.sharedInstance.GreenToRedByPercent(percentComplete);
            }
        }
    }
}

