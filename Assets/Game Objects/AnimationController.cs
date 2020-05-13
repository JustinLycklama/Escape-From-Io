using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationController : MonoBehaviour, PlayerBehaviourUpdateDelegate {
    [SerializeField]
    protected Animator animator;

    Unit.AnimationState currentState = Unit.AnimationState.Die; // Don't start at idle, so idle can be set

    PlayerBehaviour playerBehaviour;

    protected virtual void Start() {
        AnimateState(Unit.AnimationState.Idle);
        playerBehaviour = Script.Get<PlayerBehaviour>();

        playerBehaviour.RegisterForPlayerBehaviourNotifications(this);
    }

    private void OnDestroy() {
        playerBehaviour.EndPlayerBehaviourNotifications(this);
    }

    public virtual void AnimateState(Unit.AnimationState state, float rate = 1.0f, bool isCarry = false) {
        animator.speed = AnimationModifierForState(state) * rate;

        if (currentState == state) {
            return;
        }

        //print("AnimateSpeed: " + animator.speed);
        //print("Animate State: " + StringConstantForState(state, isCarry));

        animator.Play(StringConstantForState(state, isCarry));
        currentState = state;
    }

    public abstract string StringConstantForState(Unit.AnimationState state, bool isCarry = false);
    public abstract float AnimationModifierForState(Unit.AnimationState state);

    /*
     * PlayerBehaviourUpdateDelegate Interface
     * */

    private float previousAnimationSpeed = 0;

    public void PauseStateUpdated(bool paused) {
        if (paused) {
            previousAnimationSpeed = animator.speed;
            animator.speed = 0;
        } else {
            animator.speed = previousAnimationSpeed;
        }
    }
}
