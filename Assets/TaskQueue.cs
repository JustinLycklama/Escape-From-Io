using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTask {
    static int gameTaskCounter = 0;

    public int taskNumber;

    public WorldPosition target;
    public GameAction action;
    public ActionableItem actionItem;

    public GameTask(WorldPosition target, GameAction action, ActionableItem actionItem) {
        this.target = target;
        this.action = action;
        this.actionItem = actionItem;

        taskNumber = gameTaskCounter;
        gameTaskCounter++;
    }
}

public enum GameAction { Build, Destroy, Mine };

public class TaskQueue : MonoBehaviour
{
    Queue<GameTask> taskQueue;

    void Awake()
    {
        taskQueue = new Queue<GameTask>();
    }

    public void QueueBuilding() {

    }

    public void QueueTask(GameTask task) {
        taskQueue.Enqueue(task);
    }

    public int Count() {
        return taskQueue.Count;
    }

    public GameTask Pop() {
        return taskQueue.Dequeue();
    }
}
