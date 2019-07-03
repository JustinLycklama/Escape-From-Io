using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : Unit {
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Move;

    public override float SpeedForTask(GameTask gameTask) {
        switch(gameTask.action) {
            case GameTask.ActionType.Build:
                break;
            case GameTask.ActionType.Mine:
                return 0.1f;
            case GameTask.ActionType.PickUp:
                return 0.25f;
            case GameTask.ActionType.DropOff:
                return 0.5f;
            case GameTask.ActionType.FlattenPath:
                return 0.45f;
        }

        return 0.1f;
    }

    protected override void Animate() {
    }
}
