using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder : Unit {
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Build;

    public override float SpeedForTask(GameTask gameTask) {
        switch(gameTask.action) {
            case GameTask.ActionType.Build:
                return 0.1f;
            case GameTask.ActionType.Mine:
                return 0.1f;
            case GameTask.ActionType.PickUp:
                return 0.25f;
            case GameTask.ActionType.DropOff:
                break;
        }

        return 0.1f;
    }
}
