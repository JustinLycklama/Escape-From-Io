using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskDisplayCell : MonoBehaviour, ButtonDelegate {

    private MasterGameTask task;

    public GameButton linkToTaskButton;
    public GameButton cancelTaskButton;

    public Text taskDescription;

    public void Awake() {
        linkToTaskButton.buttonDelegate = this;
        cancelTaskButton.buttonDelegate = this;
    }

    public void SetTask(MasterGameTask task) {
        this.task = task;

        if(task != null) {
            taskDescription.text = task.description;
        } else {
            taskDescription.text = " - ";
        }
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
}
