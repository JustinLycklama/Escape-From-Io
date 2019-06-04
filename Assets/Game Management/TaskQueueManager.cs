using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskQueueManager : MonoBehaviour
{
    List<MasterGameTask> taskList;
    UIManager uiManager;

    void Awake()
    {
        taskList = new List<MasterGameTask>();
        uiManager = Script.Get<UIManager>();
    }

    public int Count() {
        return taskList.Count;
    }

    public void QueueTask(MasterGameTask task) {
        taskList.Add(task);
        uiManager.UpdateTaskList(taskList.ToArray());
    }

    public MasterGameTask GetNextDoableTask() {
        foreach(MasterGameTask masterTask in taskList) {
            if (masterTask.SatisfiesStartRequirements()) {

                taskList.Remove(masterTask);
                uiManager.UpdateTaskList(taskList.ToArray());

                return masterTask;
            }
        }

        return null;
    }
}
