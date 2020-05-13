using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechAnimationController : AnimationController {

    [SerializeField]
    private Animator weaponAnimator = null;

    [SerializeField]
    private ParticleSystem[] particles = new ParticleSystem[0];

    private AudioManager audioManager;

    protected override void Start() {
        base.Start();
        audioManager = Script.Get<AudioManager>();
    }

    public override void AnimateState(Unit.AnimationState state, float rate = 1.0f, bool isCarry = false) {
        if(weaponAnimator != null && state == Unit.AnimationState.PerformCoreAction &&
            weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName(StringConstantForState(state))) {
            weaponAnimator.speed = 1.0f;
            weaponAnimator.Play("Shoot_Single");

            audioManager.PlayAudio(AudioManager.Type.DefenderShot, transform.position);

            for(int i = 0; i < particles.Length; i++) {
                particles[i].Play();
            }
        } else {
            base.AnimateState(state, rate);
        }
    }

    public override string StringConstantForState(Unit.AnimationState state, bool isCarry = false) {
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
                return 5.0f;
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
