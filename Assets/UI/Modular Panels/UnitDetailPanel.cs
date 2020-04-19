using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitDetailPanel : MonoBehaviour, TimeUpdateDelegate {
    public Text unitTypeText;

    [HideInInspector]
    public PercentageBar durationBar;

    public MasterAndGameTaskCell masterAndGameTaskCell;

    public TypeValueCell mineInfo;
    public TypeValueCell moveInfo;
    public TypeValueCell buildInfo;

    private Unit unit;

    private void Awake() {
        mineInfo.type.text = "Mine Speed";
        moveInfo.type.text = "Move Speed";
        buildInfo.type.text = "Build Speed";
    }

    private void Start() {
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);
    }

    private void OnDestroy() {
        try {
            Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
        } catch(System.NullReferenceException e) { }
    }

    public void SetUnit(Unit unit) {
        this.unit = unit;

        unitTypeText.text = unit.primaryActionType.TitleAsNoun();

        mineInfo.value.text = ChanceFactory.shardInstance.ChanceFromPercent(unit.SpeedForTask(MasterGameTask.ActionType.Mine)).NameAsSkill();
        moveInfo.value.text = ChanceFactory.shardInstance.ChanceFromPercent(unit.SpeedForTask(MasterGameTask.ActionType.Move)).NameAsSkill();
        buildInfo.value.text = ChanceFactory.shardInstance.ChanceFromPercent(unit.SpeedForTask(MasterGameTask.ActionType.Build)).NameAsSkill();

        UpdatePercentBar();
    }   
           
    private void UpdatePercentBar() {
        if(unit == null) {
            return;
        }

        int remainingDuration = unit.remainingDuration;
        float percentComplete = (float)remainingDuration / (float)Unit.maxUnitUduration;

        durationBar.SetPercent(percentComplete);
        durationBar.fillColorImage.color = ColorSingleton.sharedInstance.DurationColorByPercent(percentComplete);
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {
        UpdatePercentBar();
    }
}
