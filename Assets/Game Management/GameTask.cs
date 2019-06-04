using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//public class GatherGameTask : GameTask {
//    public GatherGameTask(WorldPosition target, ActionType action, ActionableItem actionItem, PathRequestTargetType targetType = PathRequestTargetType.World) : base(target, action, actionItem, targetType) {}



//}

public class ResourceTarget<T> {

}

public class GameTask {

    public enum ActionType { Build, Mine, PickUp, Return };

    public WorldPosition target;
    private string targetDescription;
    public PathRequestTargetType pathRequestTargetType;

    // Target for Gather Action
    public GameResourceManager.GatherType gatherType = GameResourceManager.GatherType.Ore;


    public ActionType action;
    public ActionableItem actionItem;

    public MasterGameTask parentTask;

    public GameTask(WorldPosition target, ActionType action, ActionableItem actionItem, PathRequestTargetType targetType = PathRequestTargetType.World) {
        this.target = target;
        Init(action, actionItem, targetType);
    }

    public GameTask(GameResourceManager.GatherType gatherGoal, ActionType action, ActionableItem actionItem) {
        this.gatherType = gatherGoal;
        Init(action, actionItem, PathRequestTargetType.Unknown);
    }

    private void Init(ActionType action, ActionableItem actionItem, PathRequestTargetType targetType = PathRequestTargetType.World) {
        this.pathRequestTargetType = targetType;
        this.action = action;
        this.actionItem = actionItem;

        MapCoordinate mapCoordinate = new MapCoordinate(target);
        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
        targetDescription = layoutCoordinate.description;
    }
}

public class MasterGameTask {

    private static int gameTaskCounter = 0;

    // Don't know if we need a type on the master...
    public enum ActionType { Mine, Build, Gather};
    public ActionType action;

    public int taskNumber;

    // TODO: SUB TASK
    public MasterGameTask blockerTask;

    // We will create a child for each time this parent task needs to repeat
    public int repeatCount = 0;
    public List<GameTask> childTasks;

    public string description;

    public MasterGameTask(string description, GameTask[] childTasks) {

        taskNumber = gameTaskCounter;
        gameTaskCounter++;

        this.description = description;
        this.childTasks = new List<GameTask>();

        foreach (GameTask task in childTasks) {
            this.childTasks.Add(task);
            task.parentTask = this;
        }
    }

        
    //    {
    //    get {
    //        switch(action) {
    //            case ActionType.Build:
    //                return "Build " + actionItem.description;
    //            case ActionType.Mine:
    //                return "Mine Mountain at " + targetDescription;
    //            default:
    //                return "Undefined Action";
    //        }
    //    }
    //}
}
