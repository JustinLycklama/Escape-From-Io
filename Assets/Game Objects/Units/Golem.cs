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

    private const float minScale = 0.45f;
    private const float maxScale = 0.8f;

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Attack:
                return 1f;
        }

        return 0f;
    }

    public void Start() {
        animationController.IdleActivate();
    }

    public void ActiveAnimate() {
        animationController.Activate();
    }

    public void SetEvolution(float evo) {
        float scale = Mathf.Lerp(minScale, maxScale, evo);

        animationController.gameObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    protected override void AnimateState(AnimationState state, float rate = 1.0f, bool isCarry = false) {
        animationController.AnimateState(state, rate);
    }
}
