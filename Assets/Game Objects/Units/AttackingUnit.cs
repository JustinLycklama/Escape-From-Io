﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttackingUnit : Unit
{
    private Unit followingUnit;
    private UnitManager unitManager;

    protected abstract GameTask.ActionType attackType { get; }

    private Coroutine followTargetCoroutine;
    private Coroutine resetSearchCoroutine;
    private Coroutine resetTaskStateDelayed;

    private const float updateFollowTargetDelay = 0.65f;

    protected override void UnitCustomInit() {

        // Update our path to target constantly
        followTargetCoroutine = StartCoroutine(FollowAttackTarget());

        // Occasionally update our search target
        //resetSearchCoroutine = StartCoroutine(ResetSearch());

        unitManager = Script.Get<UnitManager>();

        foundWaypoints = (waypoints, actionableItem, success, distance) => {
            StopActionCoroutines();

            if(success) {
                navigatingToTask = true;

                // When requesting a path for an unknown resource (like ore) we will get the closest resource back as an actionable item

                // In this case, when we find a unit, create a task to follow that unit
                if(actionableItem != null) {
                    followingUnit = actionableItem as Unit;
                    //CreateFollowTask(actionableItem);
                    ResetTaskState();
                    return;
                }

                //movementCostToTask = distance;
                //print("Path Distance " + distance);            

                // If the task item is a known, like a location or builing, the actionItem was set at initialization
                // If the task item was an unknown resource, it has just been set above

                // In the first case, I want to let the the item know that this Master Task has an assigned unit
                // In the second, we need to alert the unknown resource that it has a new task associated
                //currentGameTask.actionItem.AssociateTask(currentMasterTask);

                Path path = new Path(waypoints, transform.position, turnDistance, stoppingDistance, currentGameTask.target);
                pathToDraw = path;


                WorldPosition worldPos = currentGameTask.target;
                MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPos);

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

                StopActionCoroutines();
                followPathCoroutine = StartCoroutine(FollowPath(path, completedPath));
            } else {
                // There is no path to task, we cannot do this.
                ResetTaskState();
            }
        };
    }

    GameTask searchTask;
    GameTask targetTask;

    public override void Shutdown() {
        base.Shutdown();

        followingUnit = null;

        if(followTargetCoroutine != null) {
            StopCoroutine(followTargetCoroutine);
        }

        if(resetSearchCoroutine != null) {
            StopCoroutine(resetSearchCoroutine);
        }    

        if(resetTaskStateDelayed != null) {
            StopCoroutine(resetTaskStateDelayed);
        }
    }

    protected override void ResetTaskState() {

        if (resetTaskStateDelayed != null) {
            StopCoroutine(resetTaskStateDelayed);
        }

        resetTaskStateDelayed = StartCoroutine(ResetTaskStateDelayed());
    }

    IEnumerator ResetTaskStateDelayed() {
        coroutinesCount["reset"]++;

        yield return new WaitForSeconds(0.75f);

        ResetTaskStateWithNewFollow();
        coroutinesCount["reset"]--;
    }

    private void ResetTaskStateWithNewFollow() {
        if (this.gameObject == null) {
            return;
        }

        currentMasterTask = null;
        currentGameTask = null;

        gameTasksQueue.Clear();

        NotifyAllTaskStatus();
        unitStatusTooltip.SetTask(this, null, null);
        unitStatusTooltip.DisplayPercentageBar(false);

        MasterGameTask masterAttackTask;

        if(unitManager.IsUnitEnabled(followingUnit)) {
            var unitPosition = new WorldPosition(followingUnit.transform.position);

            searchTask = null;
            targetTask = new GameTask(null, unitPosition, attackType, followingUnit, PathRequestTargetType.PathGrid);
            masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Attack, "Attack:\n" + followingUnit.title, new GameTask[] { targetTask }, null);
        } else {
            targetTask = null;

            // Attack opposite faction
            FactionType attackFaction = FactionType.Player;
            if (factionType == FactionType.Player) {
                attackFaction = FactionType.Enemy;
            }

            searchTask = new GameTask(null, attackFaction, attackType, null);
            masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Attack, "Looking For Target", new GameTask[] { searchTask }, null);
        }

        DoTask(masterAttackTask);
    }


    float resetSearchDelay = 6.0f;

    // If we begin chasing a unit when we are far away, then as we get closer to the group, the closest unit to us may have changed.
    // Infrequently, update the search to find the next closest unit
    IEnumerator ResetSearch() {
        while(true) {
            yield return new WaitForSeconds(resetSearchDelay);
            
            currentPathRequest.Cancel();

            if (navigatingToTask) {
                followingUnit = null;
                ResetTaskStateWithNewFollow();
            }            
        }
    }

    IEnumerator FollowAttackTarget() {
        while(true) {
            yield return new WaitForSeconds(updateFollowTargetDelay);            

            //if (factionType == FactionType.Player) {
            //    foreach(string key in coroutinesCount.Keys) {
            //        print(key + ": " + coroutinesCount[key]);
            //    }
            //}            

            // If we still have a task we are navigating to, keep updating position
            if(currentGameTask != null && currentGameTask == targetTask && followingUnit != null && unitManager.IsUnitEnabled(followingUnit) && navigatingToTask) {

                var unitPosition = new WorldPosition(followingUnit.transform.position);
                currentGameTask.target = unitPosition;

                RequestPath(transform.position, movementPenaltyMultiplier, currentGameTask, (waypoints, actionableItem, success, distance) => {

                    if(success) {
                        Path path = new Path(waypoints, transform.position, turnDistance, stoppingDistance, currentGameTask.target);
                        pathToDraw = path;


                        WorldPosition worldPos = currentGameTask.target;
                        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPos);

                        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

                        StopActionCoroutines();
                        followPathCoroutine = StartCoroutine(FollowPath(path, completedPath));
                    } else {
                        //currentPathRequest.Cancel();
                        //ResetTaskState();
                    }
                });
            }
        }
    }
}
