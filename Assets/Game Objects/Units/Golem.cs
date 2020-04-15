using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : Unit
{
    public override FactionType factionType { get { return FactionType.Enemy; } }

    public override int duration => 60 * 5;

    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Attack;

    private Narrator narrator;

    protected override void UnitCustomInit() {
        //StartCoroutine(ExecuteAfterTime(10));

        narrator = Script.Get<Narrator>();
    }

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        return 1;
        //throw new System.NotImplementedException();
    }

    protected override void Animate() {
        //throw new System.NotImplementedException();
    }


    protected override void ResetTaskState() {
        currentMasterTask = null;
        currentGameTask = null;

        gameTasksQueue.Clear();

        NotifyAllTaskStatus();
        unitStatusTooltip.SetTask(this, null);
        unitStatusTooltip.DisplayPercentageBar(false);

        //StopCoroutine(requestNextSearchTask());
        //StopCoroutine(FollowAttackTarget());

        StartCoroutine(requestNextSearchTask());
    }

    //IEnumerator DelayedInit() {
    //    yield return new WaitForSeconds(10.0f);

    //}

    float repeatFrequency = 0.25f;

    IEnumerator requestNextSearchTask() {

        yield return new WaitForSeconds(repeatFrequency);

        //yield return new WaitUntil(delegate {
        //    if(narrator == null) return false;

        //    return narrator.gameInitialized == true;
        //});

        // Setup search for new unit
        GameTask attackTask = new GameTask("Attack Robot", FactionType.Player, GameTask.ActionType.Attack, null);
        MasterGameTask masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Attack Master Task", new GameTask[] { attackTask }, null);


        foundWaypointsCompletionAction = (success) => {
            foundWaypointsCompletionAction = null;

            if(success) {
                CreateFollowTask(currentGameTask.actionItem);
            }
        };

        DoTask(masterAttackTask);

        //yield return new WaitForSeconds(repeatFrequency);

        //StartCoroutine(WaitUntilTargetFound());
    }

    //IEnumerator WaitUntilTargetFound() {

    //    yield return new WaitUntil(delegate {
    //        return currentGameTask.actionItem != null;
    //    });

    //    CreateFollowTask(currentGameTask.actionItem);
    //}

    private void CreateFollowTask(ActionableItem item) {
        //StopAllCoroutines();

        var unit = item;
        var unitPosition = new WorldPosition(unit.transform.position);

        GameTask attackTask = new GameTask("Attack: " + unit.description, unitPosition, GameTask.ActionType.Attack, unit, PathRequestTargetType.PathGrid);
        MasterGameTask masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Attack Master Task", new GameTask[] { attackTask }, null);

        currentMasterTask = null;
        currentGameTask = null;

        gameTasksQueue.Clear();

        // Continue to follow the target while the previous iteration was able to find a path
        // If iteration is not able to find a path, ResetTaskState() will be called and the cycle will start over
        foundWaypointsCompletionAction = (success) => {

            if(success) {
                StartCoroutine(FollowAttackTarget());
            } else {
                foundWaypointsCompletionAction = null;
            }
        };

        DoTask(masterAttackTask);        
    }

    IEnumerator FollowAttackTarget() {
        yield return new WaitForSeconds(repeatFrequency);

        // If we still have a task we are navigating to, keep updating position
        if (currentGameTask != null && navigatingToTask) {
            PathRequestManager.RequestPathForTask(transform.position, movementPenaltyMultiplier, currentGameTask, foundWaypoints);
        } else {
            foundWaypointsCompletionAction = null;
        }
    }
}
