using UnityEngine;
using UnityEngine.UI;

public class UnitStatusTooltip : MonoBehaviour, TrackingUIInterface { //TaskStatusUpdateDelegate

    public Text title;
    public Text taskDescription;
    public Image taskEfficiencyImage;

    public PercentageBar percentageBar;

    private RectTransform targetCanvas;
    private RectTransform rectTransform;

    // TrackingUIInterface
    public Transform toFollow { get; set; }

    //private Unit unit;

    private void Update() {
        this.UpdateTrackingPosition();
    }

    public void SetTitle(string title) {
        this.title.text = title;
    }

    public void SetTask(GameTask task) {
        if (task == null) {
            taskDescription.text = "Idle";
        } else {

            taskDescription.text = task.description;

            //switch(task.action) {
            //    case GameTask.ActionType.Mine:
            //        taskDescription.text = "Mining";
            //        break;
            //    case MasterGameTask.ActionType.Build:
            //        taskDescription.text = "Building";
            //        break;
            //    case MasterGameTask.ActionType.Move:
            //        taskDescription.text = "Gathering";
            //        break;
            //}
        }
    }

    public void DisplayPercentageBar(bool display) {
        percentageBar.gameObject.SetActive(display);
    }
}
