using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechAnimationController : AnimationController {

    [SerializeField]
    private Animator weaponAnimator;

    [SerializeField]
    private ParticleSystem[] particles = new ParticleSystem[0];

    private Unit.AnimationState weaponState = Unit.AnimationState.Idle;
    private float lastShotTime = 0;

    public override void AnimateState(Unit.AnimationState state, float rate = 1.0f) {
        base.AnimateState(state, rate);

        //if (weaponState == state) {
        //    return;
        //}

        if(weaponAnimator != null && state == Unit.AnimationState.PerformCoreAction) {
            //if (Time.fixedTime > lastShotTime + 5) {
            weaponAnimator.speed = 1.0f;
            weaponAnimator.Play("Shoot_Single");
                print("Shoot!");

                //lastShotTime = Time.fixedTime;

                for(int i = 0; i < particles.Length; i++) {
                    particles[i].Play();
                }
        }
            //}

        //weaponState = state;
    }

    public override string StringConstantForState(Unit.AnimationState state) {
        switch(state) {
            case Unit.AnimationState.Idle:
                return "Idle";
            case Unit.AnimationState.TurnLeft:
                return "Turn_45deg_L";
            case Unit.AnimationState.TurnRight:
                return "Turn_45deg_R";
            case Unit.AnimationState.Walk:
                return "Walk";
            case Unit.AnimationState.WalkTurnRight:
                break;
            case Unit.AnimationState.WalkTurnLeft:
                break;
            case Unit.AnimationState.PerformCoreAction:
                return "Idle";
            case Unit.AnimationState.Pickup:
                break;
            case Unit.AnimationState.Die:
                return "Death";
        }

        return "";
    }

    public override float AnimationModifierForState(Unit.AnimationState state) {
        switch(state) {
            case Unit.AnimationState.Idle:
                break;
            case Unit.AnimationState.TurnLeft:
            case Unit.AnimationState.TurnRight:
                return 2.5f;
            case Unit.AnimationState.Walk:
                return 5.0f;
            case Unit.AnimationState.WalkTurnRight:
                break;
            case Unit.AnimationState.WalkTurnLeft:
                break;
            case Unit.AnimationState.PerformCoreAction:
                return 10.0f;
            case Unit.AnimationState.Pickup:
                break;
            case Unit.AnimationState.Die:
                break;
        }

        return 1.0f;
    }
}
