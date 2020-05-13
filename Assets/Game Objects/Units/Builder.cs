using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder : Unit {
    public override int duration => 420;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Build;

    [SerializeField]
    private MechAnimationController mechAnimationController;

    protected override void UnitCustomInit() {

    }

    protected override void AnimateState(AnimationState state, float rate, bool isCarry = false) {
        mechAnimationController.AnimateState(state, rate);
    }

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Mine:
                return 0.3f;
            case MasterGameTask.ActionType.Build:
                return 0.65f;
            case MasterGameTask.ActionType.Move:
                return 1f;
        }

        return 0.1f;
    }
}
