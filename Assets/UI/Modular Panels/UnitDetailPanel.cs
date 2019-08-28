using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitDetailPanel : MonoBehaviour, TimeUpdateDelegate {
    public Text unitTypeText;

    [HideInInspector]
    public PercentageBar durationBar;

    public MasterAndGameTaskCell masterAndGameTaskCell;

    TypeValueCell mineInfo;
    TypeValueCell moveInfo;
    TypeValueCell buildneInfo;

    private Unit unit;

    private void Awake() {
        mineInfo.type.text = "Mine Speed";
        mineInfo.type.text = "Move Speed";
        mineInfo.type.text = "Build Speed";
    }

    private void Start() {
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);
    }

    private void OnDestroy() {
        Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
    }

    public void SetUnit(Unit unit) {
        this.unit = unit;

        unitTypeText.text = unit.primaryActionType.TitleAsNoun();

        mineInfo.value = 

        UpdatePercentBar();
    }   
           
    private void UpdatePercentBar() {
        if(unit == null) {
            return;
        }

        int remainingDuration = unit.remainingDuration;
        float percentComplete = (float)remainingDuration / (float)Unit.maxUnitUduration;

        durationBar.SetPercent(percentComplete);
        durationBar.fillColorImage.color = ColorSingleton.sharedInstance.GreenToRedByPercent(percentComplete);
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {
        UpdatePercentBar();
    }
}
