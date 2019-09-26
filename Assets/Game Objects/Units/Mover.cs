using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : Unit {
    public override int duration => 20;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Move;

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Mine:
                return 0.3f;
            case MasterGameTask.ActionType.Build:
                return 0.3f;
            case MasterGameTask.ActionType.Move:
                return 1;
        }

        return 0.1f;
    }

    protected override void Animate() {

    }
}
