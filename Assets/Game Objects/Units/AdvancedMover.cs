﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedMover : Unit {
    public override int duration => 360;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Move;

    [SerializeField]
    private DroneAnimationController animationController = null;

    protected override void UnitCustomInit() {

    }

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Attack:
                return 0.3f;
            case MasterGameTask.ActionType.Build:
                return 0.3f;
            case MasterGameTask.ActionType.Move:
                return 2;
        }

        return 0.1f;
    }

    protected override void AnimateState(AnimationState state, float rate = 1.0f, bool isCarry = false) {
        animationController.AnimateState(state, rate, isCarry);
    }
}

