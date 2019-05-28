using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Task {

    public WorldPosition target;

    public Task(WorldPosition target) {
        this.target = target;
    }
}

public class TaskQueue : MonoBehaviour
{
    Queue<Task> taskQueue;

    void Awake()
    {
        taskQueue = new Queue<Task>();
    }

    public void QueueBuilding() {

    }

    public void QueueTask(Task task) {
        taskQueue.Enqueue(task);
    }

    public int Count() {
        return taskQueue.Count;
    }

    public Task Pop() {
        return taskQueue.Dequeue();
    }
}
