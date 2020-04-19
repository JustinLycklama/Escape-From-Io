using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthElementalController : AnimationController
{
    // Start is called before the first frame update
    void Start() {
        //animator.speed = 1.5f;
    }

    // Base Animation Actions
    public override void Idle() {
        animator.speed = 1.0f;
        animator.Play("idle");
    }

    public override void Walk() {
        animator.speed = 1.5f;
        animator.Play("walk");
    }

    public override void Hit() {
        animator.Play("hit");
    }


    public override void Atk01() {
        animator.speed = 1.0f;
        animator.Play("attack01");
    }

    public override void Die() {
        animator.Play("die");
    }

    // Extra Actions
    public void Atk02() {
        animator.speed = 1.0f;
        animator.Play("attack02");
    }

    public void Activate() {
        animator.Play("activate");
    }

    public void IdleActivate() {
        animator.Play("idleActivate");
    }

    public void Run() {
        animator.Play("run");
    }
}
