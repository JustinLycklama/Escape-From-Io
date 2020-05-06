using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationController : MonoBehaviour
{
    [SerializeField]
    protected Animator animator;

    Unit.AnimationState currentState = Unit.AnimationState.Die; // Don't start at idle, so idle can be set

    private void Start() {
        AnimateState(Unit.AnimationState.Idle);
    }

    public virtual void AnimateState(Unit.AnimationState state, float rate = 1.0f) {
        animator.speed = AnimationModifierForState(state) * rate;

        if (currentState == state) {
            return;
        }

        animator.Play(StringConstantForState(state));
        currentState = state;
    }

    public abstract string StringConstantForState(Unit.AnimationState state);
    public abstract float AnimationModifierForState(Unit.AnimationState state);
}
