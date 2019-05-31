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

    public string description {
        get {
            switch(action) {
                case GameAction.Build:
                    return "Build " + actionItem.description;
                case GameAction.Mine:                    
                default:
                    return "";
            }
        }
    }
}

public enum GameAction { Build, Mine };

public class TaskQueue : MonoBehaviour
{
    Queue<GameTask> taskQueue;
    UIManager uiManager;

    void Awake()
    {
        taskQueue = new Queue<GameTask>();
        uiManager = Script.Get<UIManager>();
    }

    public int Count() {
        return taskQueue.Count;
    }

    public void QueueBuilding() {

    }

    public void QueueTask(GameTask task) {
        taskQueue.Enqueue(task);
        uiManager.UpdateTaskList(taskQueue.ToArray());
    }

    public GameTask Pop() {
        GameTask task = taskQueue.Dequeue();
        uiManager.UpdateTaskList(taskQueue.ToArray());
        return task;
    }
}
