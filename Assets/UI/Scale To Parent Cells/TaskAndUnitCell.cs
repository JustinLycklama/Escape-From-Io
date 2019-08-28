using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class TaskAndUnitCell : MonoBehaviour, IPointerClickHandler, TaskQueueDelegate, UnitManagerDelegate, ButtonDelegate, TimeUpdateDelegate {

    public Text title;

    public Text unitCountText;
    public Text unitStatusText;

    public Text taskCountText;
    public Toggle taskListLocked;

    public MasterGameTask.ActionType actionType;
    public Image backgroundSprite;

    public List<PercentageBar> percentBars;
    private List<Unit> soonToExpireUnits = new List<Unit>();

    private void Start() {
        title.text = actionType.ToString();

        taskListLocked.buttonDelegate = this;

        Script.Get<TaskQueueManager>().RegisterForNotifications(this, actionType);
        Script.Get<UnitManager>().RegisterForNotifications(this, actionType);
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);

        SecondUpdated();
    }

    private void OnDestroy() {
        Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
        Script.Get<UnitManager>().EndNotifications(this, actionType);
        Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if (button == taskListLocked) {
            taskListLocked.SetState(!taskListLocked.state);

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
        taskCountText.text = taskList.Length + " Task" + ((taskList.Length == 1)? "" : "s");

        taskListLocked.SetState(Script.Get<TaskQueueManager>().GetTaskListLockStatus(actionType));
    }

    /*
     * UnitManagerDelegate Interface
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType, Unit.UnitState unitListState) {
        unitCountText.text = unitList.Length + " Unit" + ((unitList.Length == 1) ? "" : "s");
        unitStatusText.text = unitListState.decription();

        backgroundSprite.color = unitListState.ColorForState();

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

                print(remainingDuration);

                bar.SetPercent(percentComplete);
                bar.fillColorImage.color = ColorSingleton.sharedInstance.GreenToRedByPercent(percentComplete);
            }
        }
    }
}

