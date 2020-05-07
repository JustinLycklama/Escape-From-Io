using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitDisplayCell : MonoBehaviour, TaskStatusUpdateDelegate, GameButtonDelegate {

    //public Text unitTitleText;
    public Text unitTypeText;

    public GameButton cellButton;

    [SerializeField]
    private PercentageBar durationBar;
    [SerializeField]
    private PercentageBar healthBar;

    [SerializeField]
    public MasterAndGameTaskCell masterAndGameTaskCell;


    private Unit unit;

    private void Awake() {
        cellButton.buttonDelegate = this;
    }

    public void SetUnit(Unit unit) {

        // If we were watching an old unit, stop
        if (this.unit != null) {
            unit.EndTaskStatusNotifications(this);
        }

        this.unit = unit;
        unit.RegisterForTaskStatusNotifications(this);

        //unitTitleText.text = unit.description;
        unitTypeText.text = unit.primaryActionType.TitleAsNoun();
    }

    private void OnDestroy() {
        if (unit != null) {
            unit.EndTaskStatusNotifications(this);
        }        
    }

    /*
     * TaskStatusUpdateDelegate Interface
     * */

    public void NowPerformingTask(Unit unit, MasterGameTask masterGameTask, GameTask gameTask) {
        masterAndGameTaskCell.SetTask(masterGameTask, gameTask);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        Script.Get<PlayerBehaviour>().PanCameraToUnit(unit);
        //Script.Get<SelectionManager>().SelectSelectable(unit);
    }
}
