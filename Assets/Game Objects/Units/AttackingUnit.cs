using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttackingUnit : Unit
{
    private Narrator narrator;

    private Unit followingUnit;
    private UnitManager unitManager;

    protected abstract GameTask.ActionType attackType { get; }

    protected override void UnitCustomInit() {

        // Update our path to target constantly
        StartCoroutine(FollowAttackTarget());

        // Occasionally update our search target
        StartCoroutine(ResetSearch());

        narrator = Script.Get<Narrator>();
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

                FollowPathCoroutine = StartCoroutine(FollowPath(path, completedPath));
            } else {
                // There is no path to task, we cannot do this.
                ResetTaskState();
            }
        };
    }

    GameTask searchTask;
    GameTask targetTask;

    protected override void ResetTaskState() {
        StartCoroutine(ResetTaskStateDelayed());
    }

    IEnumerator ResetTaskStateDelayed() {
        yield return new WaitForSeconds(0.75f);

        ResetTaskStateWithNewFollow();
    }


    private void ResetTaskStateWithNewFollow() {
        if (this == null) {
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

    float updateFollowTargetDelay = 0.30f;

    IEnumerator FollowAttackTarget() {

        while(true) {
            yield return new WaitForSeconds(updateFollowTargetDelay);

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
                        FollowPathCoroutine = StartCoroutine(FollowPath(path, completedPath));
                    } else {
                        currentPathRequest.Cancel();
                        //ResetTaskState();
                    }
                });
            }
        }
        
    }
}
