using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder : Unit {
    public override int duration => 120;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Build;

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Mine:
                return 0.3f;
            case MasterGameTask.ActionType.Build:
                return 0.75f;
            case MasterGameTask.ActionType.Move:
                return 0.3f;
        }

        return 0.1f;
    }

    protected override void Animate() {
        
    }
}
