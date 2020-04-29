using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : AttackingUnit
{
    [SerializeField]
    private EarthElementalController animationController;

    public override FactionType factionType { get { return FactionType.Enemy; } }

    public override int duration => 60 * 5;

    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Attack;

    protected override GameTask.ActionType attackType => GameTask.ActionType.AttackMele;

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Attack:
                return 1.5f;
        }

        return 0f;
    }

    protected override void Animate() {
        //throw new System.NotImplementedException();
    }

    public void Start() {
        animationController.IdleActivate();
    }

    public void ActiveAnimate() {
        animationController.Activate();
    }

    //protected override void UnitCustomInit() {
    //    base.UnitCustomInit();

    //    //animationController.Idle();
    //}

    /*
     * Action Delegates
     * */

    protected override void BeginWalkDelegate() {
        base.BeginWalkDelegate();

        animationController.Walk();
    }

    protected override void CompleteWalkDelegate() {
        base.CompleteWalkDelegate();

        animationController.Idle();
    }

    protected override void BeginTaskActionDelegate() {
        base.BeginTaskActionDelegate();

        if(NoiseGenerator.random.Next(0, 2) == 0) {
            animationController.Atk01();
        } else {
            animationController.Atk02();
        }
    }

    protected override void CompleteTaskActionDelegate() {

    }
}
