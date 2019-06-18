using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TaskAndUnitCell : MonoBehaviour, IPointerClickHandler, TaskQueueDelegate {

    public Text title;

    public Text unitText;
    public Text taskText;

    public Image unitTypeImage;
    public Image currentStatusImage;

    public MasterGameTask.ActionType actionType;

    private void Start() {
        title.text = actionType.ToString();
        unitText.text = "1 Unit";

        Script.Get<TaskQueueManager>().RegisterForNotifications(this, actionType);
    }

    private void OnDestroy() {
        Script.Get<TaskQueueManager>().EndNotifications(this, actionType);
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

    public void NotifyUpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType actionType) {
        taskText.text = taskList.Length + " " + actionType.ToString() + " Task" + ((taskList.Length == 1)? "" : "s");
    }
}
