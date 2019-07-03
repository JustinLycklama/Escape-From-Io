using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ore : ActionableItem {
    public string description => throw new NotImplementedException();

    public Unit currentCarrier;

    float actionPercent = 0;

    
    //public Unit associatedUnit;
    public GameTask associatedTask;
    //public override void AssociateTask(GameTask task) {
    //    associatedTask = task;
    //}

    public override float performAction(GameTask task, float rate, Unit unit) {
        switch(task.action) {

            case GameTask.ActionType.PickUp:
                actionPercent += rate;

                if (actionPercent >= 1) {
                    actionPercent = 1;

                    // The associatedTask is over
                    associatedTask = null;

                    GameResourceManager resourceManager = Script.Get<GameResourceManager>();
                    resourceManager.GiveToUnit(this, unit);
                    this.transform.SetParent(unit.transform, true);

                    actionPercent = 0;

                    return 1;
                }

                break;
            case GameTask.ActionType.DropOff:



                break;

            case GameTask.ActionType.Build:
            case GameTask.ActionType.Mine:
            default:
                throw new NotImplementedException();
        }

        return actionPercent;
    }
}
