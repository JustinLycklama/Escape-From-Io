using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Defender : AttackingUnit
{
    public override FactionType factionType { get { return FactionType.Player; } }

    public override int duration => 60 * 5;

    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Attack;

    protected override GameTask.ActionType attackType => GameTask.ActionType.AttackRanged;

    [SerializeField]
    private MechAnimationController mechAnimationController;

    protected override void AnimateState(AnimationState state, float rate, bool isCarry = false) {
        mechAnimationController.AnimateState(state, rate);
    }

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Attack:
                return 2f;
        }

        return 0f;
    }

    ///*
    // * Action Delegates
    // * */

    //protected override void BeginWalkDelegate() {
    //    base.BeginWalkDelegate();

    //    //animationController.Walk();
    //}

    //protected override void CompleteWalkDelegate() {
    //    base.CompleteWalkDelegate();

    //    //animationController.Idle();
    //}

    //protected override void BeginTaskActionDelegate() {
    //    base.BeginTaskActionDelegate();

    //    //if(NoiseGenerator.random.Next(0, 2) == 0) {
    //    //    animationController.Atk01();
    //    //} else {
    //    //    animationController.Atk01();
    //        //animationController.Atk02();
    //    //}
    //}
}
