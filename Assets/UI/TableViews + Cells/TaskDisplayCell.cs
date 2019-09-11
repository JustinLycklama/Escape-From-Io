using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskDisplayCell : MonoBehaviour, GameButtonDelegate, MasterTaskUpdateDelegate {

    private MasterGameTask task;

    public GameButton linkToTaskButton;
    public GameButton cancelTaskButton;

    public Text taskDescription;
    public Text taskRepeatText;


    public void Awake() {
        linkToTaskButton.buttonDelegate = this;
        cancelTaskButton.buttonDelegate = this;
    }

    public void SetTask(MasterGameTask task) {
        if(this.task != null) {
            task.EndTaskStatusNotifications(this);
        }

        this.task = task;

        if(task != null) {
            taskDescription.text = task.description;
            task.RegisterForTaskStatusNotifications(this);
        } else {
            taskDescription.text = " - ";
        }

        cancelTaskButton.SetEnabled(task.CancellableByUI);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if (button == linkToTaskButton) {
            PlayerBehaviour playerBehaviour = Script.Get<PlayerBehaviour>();

            playerBehaviour.JumpCameraToTask(task);
        } else if (button == cancelTaskButton) {
            task.CancelTask();

            //TaskQueueManager taskQueueManager = Script.Get<TaskQueueManager>();

            //taskQueueManager.DeQueueTask(task);
        }
    }

    /*
    * MasterTaskUpdateDelegate Interface
    * */

    public void RepeatCountUpdated(MasterGameTask masterGameTask, int count) {
        bool enabled = count > 1;        

        if (taskRepeatText.gameObject.activeSelf != enabled) {
            taskRepeatText.gameObject.SetActive(enabled);
        }

        taskRepeatText.text = "(x" + count + ")";
    }

    public void TaskCancelled(MasterGameTask masterGameTask) { }
    public void TaskFinished(MasterGameTask masterGameTask) { }
}
