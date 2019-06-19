using UnityEngine;
using UnityEngine.UI;

public class UnitStatusTooltip : TrackingUIElement { //TaskStatusUpdateDelegate

    public Text title;
    public Text taskDescription;
    public Image taskEfficiencyImage;

    public PercentageBar percentageBar;

    private RectTransform targetCanvas;
    private RectTransform rectTransform;

    //private Unit unit;

    public void SetTitle(string title) {
        this.title.text = title;
    }

    public void SetTask(MasterGameTask task) {
        if (task == null) {
            taskDescription.text = "Idle...";
        } else {
            switch(task.actionType) {
                case MasterGameTask.ActionType.Mine:
                    taskDescription.text = "Mining";
                    break;
                case MasterGameTask.ActionType.Build:
                    taskDescription.text = "Building";
                    break;
                case MasterGameTask.ActionType.Move:
                    taskDescription.text = "Gathering";
                    break;
            }
        }
    }

    public void DisplayPercentageBar(bool display) {
        percentageBar.gameObject.SetActive(display);
    }
}
