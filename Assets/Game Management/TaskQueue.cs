using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTask {
    static int gameTaskCounter = 0;

    public int taskNumber;
    
    public WorldPosition target;
    private string targetDescription;
    public PathRequestTargetType targetType;

    public GameAction action;
    public ActionableItem actionItem;

    public GameTask(WorldPosition target, GameAction action, ActionableItem actionItem, PathRequestTargetType targetType = PathRequestTargetType.World) {
        this.target = target;       
        this.targetType = targetType;
        this.action = action;
        this.actionItem = actionItem;

        taskNumber = gameTaskCounter;
        gameTaskCounter++;

        MapCoordinate mapCoordinate = new MapCoordinate(target);
        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
        targetDescription = layoutCoordinate.Description();
    }

    public string description {
        get {
            switch(action) {
                case GameAction.Build:
                    return "Build " + actionItem.description;
                case GameAction.Mine:
                    return "Mine Mountain at " + targetDescription;
                default:
                    return "Undefined Action";
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
