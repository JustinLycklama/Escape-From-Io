using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneAnimationController : AnimationController {
    public override string StringConstantForState(Unit.AnimationState state, bool isCarry = false) {
        switch(state) {
            case Unit.AnimationState.Idle:
                return isCarry ? "Carry_Idle" : "Idle";
            case Unit.AnimationState.TurnLeft:
            case Unit.AnimationState.TurnRight:
            case Unit.AnimationState.Walk:
            case Unit.AnimationState.WalkTurnRight:                
            case Unit.AnimationState.WalkTurnLeft:
                return isCarry ? "Carry_Move" : "Move";                
            case Unit.AnimationState.PerformCoreAction:
            case Unit.AnimationState.Pickup:
                return isCarry ? "Idle" : "Pick_Up";
            case Unit.AnimationState.Die:
                return "Die";
        }

        return "";
    }

    public override float AnimationModifierForState(Unit.AnimationState state) {
        switch(state) {
            case Unit.AnimationState.Idle:
                break;
            case Unit.AnimationState.TurnLeft:
            case Unit.AnimationState.TurnRight:
            case Unit.AnimationState.Walk:
            case Unit.AnimationState.WalkTurnRight:
            case Unit.AnimationState.WalkTurnLeft:
                return 3.0f;
            case Unit.AnimationState.PerformCoreAction:
            case Unit.AnimationState.Pickup:
                break;
            case Unit.AnimationState.Die:
                break;
        }

        return 1.0f;
    }
}
