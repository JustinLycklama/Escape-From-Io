using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UCharts;
using System.Linq;

public class AttackersCell : MonoBehaviour, UnitManagerDelegate, GameButtonDelegate, TimeUpdateDelegate {
    [SerializeField]
    private List<PercentageBar> defenderPercentBars;

    [SerializeField]
    private HalfPieChart frequencyChart;

    [SerializeField]
    private HalfPieChart evolutionChart;

    private EnemyManager enemyManager;

    private List<Unit> soonToExpireUnits = new List<Unit>();

    private const MasterGameTask.ActionType actionType = MasterGameTask.ActionType.Attack;


    // Start is called before the first frame update
    void Start()
    {
        enemyManager = Script.Get<EnemyManager>();

        Script.Get<UnitManager>().RegisterForNotifications(this, actionType);
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);
    }

    private void OnDestroy() {
        try {
            Script.Get<UnitManager>().EndNotifications(this, actionType);
            Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
        } catch(System.NullReferenceException e) { }
    }

    /*
    * ButtonDelegate Interface
    * */

    public void ButtonDidClick(GameButton button) {

        //if(button == taskListLockButton) {
        //    TaskQueueManager taskQueueManager = Script.Get<TaskQueueManager>();
        //    var newState = !taskQueueManager.GetTaskListLockStatus(actionType);

        //    SetLockState(newState);
        //    taskQueueManager.SetTaskListLocked(actionType, newState);
        //}
    }

    /*
     * UnitManagerDelegate Interface
     * */

    public void NotifyUpdateUnitList(Unit[] unitList, MasterGameTask.ActionType actionType) {
        soonToExpireUnits = unitList.OrderBy(unit => unit.remainingDuration).Take(defenderPercentBars.Count).ToList();
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {

        // Attackers modifiers
        int frequencyPercent = Mathf.Clamp(Mathf.RoundToInt(enemyManager.frequency * 100), 0, 100);
        frequencyChart.SetData(new List<PieChartDataNode> { new PieChartDataNode("", frequencyPercent), new PieChartDataNode("", 100 - frequencyPercent) });

        int evolutionPercent = Mathf.Clamp(Mathf.RoundToInt(enemyManager.evolution * 100), 0, 100);
        evolutionChart.SetData(new List<PieChartDataNode> { new PieChartDataNode("", evolutionPercent), new PieChartDataNode("", 100 - evolutionPercent) });

        // Defender's Percent bars
        for(int i = 0; i < defenderPercentBars.Count; i++) {
            PercentageBar bar = defenderPercentBars[i];

            bool activate = soonToExpireUnits.Count > i;
            if(bar.gameObject.activeSelf != activate) {
                bar.gameObject.SetActive(activate);
            }

            if(soonToExpireUnits.Count > i) {
                int remainingDuration = soonToExpireUnits[i].remainingDuration;
                float percentComplete = (float)remainingDuration / (float)Unit.maxUnitUduration;

                bar.SetPercent(percentComplete);
                bar.fillColorImage.color = ColorSingleton.sharedInstance.DurationColorByPercent(percentComplete);
            }
        }
    }

}
