using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MasterAndGameTaskCell : MonoBehaviour
{
    public Text taskDescription;

    MasterGameTask task;
    GameTask gameTask;

    private string defaultText = "No Current Task"; 

    private void Awake() {
        taskDescription.text = defaultText;
    }

    public void SetTask(MasterGameTask task, GameTask gameTask) {
        this.task = task;
        this.gameTask = gameTask;



        if (task == null && gameTask == null) {
            taskDescription.text = defaultText;
        } else {
            string description = "";

            if (task != null) {
                description += task.description;
            }

            if (gameTask != null) {
                description += (description.Length > 0) ? " - " : "" + gameTask.description;
            }

            taskDescription.text = description;
        }
    }
}
