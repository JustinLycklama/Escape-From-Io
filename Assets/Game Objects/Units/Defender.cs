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
    private MechAnimationController mechAnimationController = null;

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

}
