using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : Unit {
    public override int duration => 280;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Move;

    [SerializeField]
    private MechAnimationController mechAnimationController = null;

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
                return 0.3f;
            case MasterGameTask.ActionType.Move:
                return 1;
        }

        return 0.1f;
    }
}
