using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthElementalController : AnimationController
{
    [SerializeField]
    private GameObject mainInstanceMesh;

    [SerializeField]
    private GameObject instanceDieMesh;

    public void Activate() {
        animator.Play("activate");
    }

    public void IdleActivate() {
        animator.Play("idleActivate");
    }

    public void DieEvent() {

        instanceDieMesh.SetActive(true);

        GameObject dieMesh = (GameObject)Instantiate(instanceDieMesh, transform.position, transform.rotation);
        dieMesh.transform.localScale = transform.localScale;
        dieMesh.transform.parent = transform.parent.parent;
        dieMesh.SetActive(true);

        mainInstanceMesh.SetActive(false);

        dieMesh.transform.localScale = new Vector3(1, 1, 1);

        //if(Demo.curmat == 0) {
        //    GameObject G = (GameObject)Instantiate(Demo.DieAnim[0], Istancer.position, Istancer.rotation);
        //    Demo.instanceDieMesh = G;
        //    Demo.instanceDieMesh.transform.parent = Demo.T;
        //    G.SetActive(true);
        //} else {
        //    GameObject G = (GameObject)Instantiate(Demo.DieAnim[1], Istancer.position, Istancer.rotation);
        //    Demo.instanceDieMesh = G;
        //    G.SetActive(true);
        //}
        //EartElemental.SetActive(false);
    }

    public override string StringConstantForState(Unit.AnimationState state) {
        switch(state) {
            case Unit.AnimationState.Idle:
                return "idle";
            case Unit.AnimationState.TurnLeft:
                break;
            case Unit.AnimationState.TurnRight:
                break;
            case Unit.AnimationState.Walk:
                return "walk";
            case Unit.AnimationState.WalkTurnRight:
                break;
            case Unit.AnimationState.WalkTurnLeft:
                break;
            case Unit.AnimationState.PerformCoreAction:
                if(NoiseGenerator.random.Next(0, 2) == 0) {
                    return "attack01";
                } else {
                    return "attack02";
                }
            case Unit.AnimationState.Pickup:
                break;
            case Unit.AnimationState.Die:
                return "die";
        }

        return "";
    }

    public override float AnimationModifierForState(Unit.AnimationState state) {
        switch(state) {
            case Unit.AnimationState.Idle:
                break;
            case Unit.AnimationState.TurnLeft:
                break;
            case Unit.AnimationState.TurnRight:
                break;
            case Unit.AnimationState.Walk:
                return 1.5f;
            case Unit.AnimationState.WalkTurnRight:
                break;
            case Unit.AnimationState.WalkTurnLeft:
                break;
            case Unit.AnimationState.PerformCoreAction:
                break;
            case Unit.AnimationState.Pickup:
                break;
            case Unit.AnimationState.Die:
                break;
        }

        return 1.0f;
    }
}
