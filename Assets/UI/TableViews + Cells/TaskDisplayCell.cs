using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskDisplayCell : MonoBehaviour {

    private MasterGameTask task;
    public Text taskDescription;

    public void SetTask(MasterGameTask task) {
        this.task = task;

        if(task != null) {
            taskDescription.text = task.description;
        } else {
            taskDescription.text = " - ";
        }
    }
}
