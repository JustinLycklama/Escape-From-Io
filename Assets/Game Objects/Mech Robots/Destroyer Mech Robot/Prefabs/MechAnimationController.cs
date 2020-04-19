using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MechAnimationController : AnimationController {

    // Base Animation Actions
    public override void Idle() {
        animator.speed = 1.0f;
        animator.Play("idle1");
    }

    public override void Walk() {
        animator.speed = 1.5f;
        animator.Play("walk");
    }

    public override void Hit() {
        animator.Play("hit1");
    }

    public override void Atk01() {
        animator.speed = 1.0f;
        animator.Play("stand attack left");
    }

    public override void Die() {
        animator.Play("death fall back");
    }

    // Extra Actions
}
