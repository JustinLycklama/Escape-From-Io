using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationController : MonoBehaviour
{
    [SerializeField]
    protected Animator animator;

    public abstract void Idle();

    public abstract void Walk();

    public abstract void Hit();

    public abstract void Atk01();

    public abstract void Die();
}
