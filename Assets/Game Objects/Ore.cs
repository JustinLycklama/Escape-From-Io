using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ore : ActionableItem {
    public override string description => mineralType.ToString();

    public Unit currentCarrier;

    float actionPercent = 0;

    public MineralType mineralType = MineralType.Copper;
   
    public override float performAction(GameTask task, float rate, Unit unit) {
        switch(task.action) {

            case GameTask.ActionType.PickUp:
                actionPercent += rate;

                if (actionPercent >= 1) {
                    actionPercent = 1;

                    // The associatedTask is over
                    AssociateTask(null);                    

                    GameResourceManager resourceManager = Script.Get<GameResourceManager>();
                    resourceManager.GiveToUnit(this, unit);

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
