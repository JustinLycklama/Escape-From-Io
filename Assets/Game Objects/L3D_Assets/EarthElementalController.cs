using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthElementalController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    // Start is called before the first frame update
    void Start() {
        //animator.speed = 1.5f;
    }

    public void Idle() {
        animator.speed = 1.0f;
        animator.Play("idle");
    }

    public void IdleActivate() {
        animator.Play("idleActivate");
    }

    public void Walk() {
        animator.speed = 1.5f;
        animator.Play("walk");
    }

    public void Run() {
        animator.Play("run");
    }

    public void Hit() {
        animator.Play("hit");
    }

    public void Activate() {
        animator.Play("activate");
    }

    public void Atk01() {
        animator.speed = 1.0f;
        animator.Play("attack01");
    }

    public void Atk02() {
        animator.speed = 1.0f;
        animator.Play("attack02");
    }

    public void die() {
        animator.Play("die");
    }
}
