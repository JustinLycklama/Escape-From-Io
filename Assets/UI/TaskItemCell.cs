using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskItemCell : MonoBehaviour
{
    public Text taskDescription;

    GameTask task;

    private string defaultText = "No Current Task"; 

    private void Awake() {
        taskDescription.text = defaultText;
    }

    public void SetTask(GameTask task) {
        this.task = task;

        if (task != null) {
            taskDescription.text = task.description;
        } else {
            taskDescription.text = defaultText;
        }
        
    }

}
