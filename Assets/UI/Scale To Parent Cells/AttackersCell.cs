using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UCharts;
using System.Linq;
using UnityEngine.UI;

public class AttackersCell : MonoBehaviour, UnitManagerDelegate, GameButtonDelegate, TimeUpdateDelegate {

    [SerializeField]
    private Text unitsCountText = null;

    [SerializeField]
    private List<PercentageBar> defenderPercentBars = null;

    [SerializeField]
    private Text enemyCountText = null;

    [SerializeField]
    private HalfPieChart frequencyChart = null;

    [SerializeField]
    private HalfPieChart evolutionChart = null;

    private List<Unit> soonToExpireUnits = new List<Unit>();

    private const MasterGameTask.ActionType actionType = MasterGameTask.ActionType.Attack;


    // Start is called before the first frame update
    void Start()
    {
        Script.Get<UnitManager>().RegisterForNotifications(this, actionType);
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);
    }

    private void OnDestroy() {
        try {
            Script.Get<UnitManager>().EndNotifications(this, actionType);
            Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
        } catch(System.NullReferenceException) { }
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

        Unit[] playerUnits = unitList.Where(u => { return u.factionType == Unit.FactionType.Player; }).ToArray();
        soonToExpireUnits = playerUnits.OrderBy(unit => unit.remainingDuration).Take(defenderPercentBars.Count).ToList();
        unitsCountText.text = playerUnits.Length.ToString();

        Unit[] enemyUnits = unitList.Where(u => { return u.factionType == Unit.FactionType.Enemy; }).ToArray();
        enemyCountText.text = enemyUnits.Length.ToString();
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {

        // Attackers modifiers
        int frequencyPercent = Mathf.Clamp(Mathf.RoundToInt(EnemyManager.frequency * 100), 0, 100);
        frequencyChart.SetData(new List<PieChartDataNode> { new PieChartDataNode("", frequencyPercent), new PieChartDataNode("", 100 - frequencyPercent) });

        int evolutionPercent = Mathf.Clamp(Mathf.RoundToInt(EnemyManager.evolution * 100), 0, 100);
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
