using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : Unit {
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Mine;

    public override float SpeedForTask(GameTask gameTask) {
        switch(gameTask.action) {
            case GameTask.ActionType.Build:
                break;
            case GameTask.ActionType.Mine:
                return 0.1f;
            case GameTask.ActionType.PickUp:
                break;
            case GameTask.ActionType.DropOff:
                break;
        }

        return 0.1f;
    }
}
