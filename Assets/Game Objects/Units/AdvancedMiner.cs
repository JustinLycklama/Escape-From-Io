using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedMiner : Unit {
    public override int duration => 600;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Mine;

    [SerializeField]
    private MechAnimationController mechAnimationController = null;

    protected override void AnimateState(AnimationState state, float rate, bool isCarry = false) {
        mechAnimationController.AnimateState(state, rate);
    }

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Mine:
                return 2f;
            case MasterGameTask.ActionType.Build:
                return 0.3f;
            case MasterGameTask.ActionType.Move:
                return 1f;
        }

        return 0.1f;
    }

    protected override void UnitCustomInit() {

    }
}
