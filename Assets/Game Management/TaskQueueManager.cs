using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskQueueManager : MonoBehaviour
{
    Queue<MasterGameTask> taskQueue;
    UIManager uiManager;

    void Awake()
    {
        taskQueue = new Queue<MasterGameTask>();
        uiManager = Script.Get<UIManager>();
    }

    public int Count() {
        return taskQueue.Count;
    }

    public void QueueTask(MasterGameTask task) {
        taskQueue.Enqueue(task);
        uiManager.UpdateTaskList(taskQueue.ToArray());
    }

    public MasterGameTask GetNextDoableTask() {
        if (taskQueue.Count == 0) {
            return null ;
        }

        MasterGameTask task = taskQueue.Dequeue();
        uiManager.UpdateTaskList(taskQueue.ToArray());

        return task;
    }
}
