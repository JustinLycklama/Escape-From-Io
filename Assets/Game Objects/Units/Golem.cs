using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : Unit
{
    public override FactionType factionType { get { return FactionType.Enemy; } }

    public override int duration => 60 * 5;

    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Attack;

    private Narrator narrator;

    private Unit followingUnit;
    private UnitManager unitManager;

    protected override void UnitCustomInit() {
        //StartCoroutine(ExecuteAfterTime(10));

        StartCoroutine(FollowAttackTarget());


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

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        return 1;
        //throw new System.NotImplementedException();
    }

    protected override void Animate() {
        //throw new System.NotImplementedException();
    }


    GameTask searchTask;
    GameTask targetTask;

    protected override void ResetTaskState() {
        StartCoroutine(ResetTaskStateDelayed());
    }

    IEnumerator ResetTaskStateDelayed() {
        yield return new WaitForSeconds(0.75f);

        currentMasterTask = null;
        currentGameTask = null;

        gameTasksQueue.Clear();

        NotifyAllTaskStatus();
        unitStatusTooltip.SetTask(this, null);
        unitStatusTooltip.DisplayPercentageBar(false);

        MasterGameTask masterAttackTask;

        if(unitManager.IsUnitEnabled(followingUnit)) {
            var unitPosition = new WorldPosition(followingUnit.transform.position);

            searchTask = null;
            targetTask = new GameTask("Attack: " + followingUnit.description, unitPosition, GameTask.ActionType.Attack, followingUnit, PathRequestTargetType.PathGrid);
            masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Attack Master Task", new GameTask[] { targetTask }, null);
        } else {
            targetTask = null;
            searchTask = new GameTask("Attack Robot", FactionType.Player, GameTask.ActionType.Attack, null);
            masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Attack Master Task", new GameTask[] { searchTask }, null);
        }


        DoTask(masterAttackTask);
    }

    //IEnumerator requestNextSearchTask() {

    //    yield return new WaitForSeconds(repeatFrequency);

    //    print("Search for Any Unit to follow");

    //    // Setup search for new unit
    //    targetTask = null;
    //    searchTask = new GameTask("Attack Robot", FactionType.Player, GameTask.ActionType.Attack, null);
    //    MasterGameTask masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Attack Master Task", new GameTask[] { searchTask }, null);

    //    DoTask(masterAttackTask);
    //}

    //private void CreateFollowTask(ActionableItem item) {

    //    print("Create New Follow Request Path");

    //    var unit = item;
    //    var unitPosition = new WorldPosition(unit.transform.position);

    //    searchTask = null;
    //    targetTask = new GameTask("Attack: " + unit.description, unitPosition, GameTask.ActionType.Attack, unit, PathRequestTargetType.PathGrid);
    //    MasterGameTask masterAttackTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Attack Master Task", new GameTask[] { targetTask }, null);

    //    currentMasterTask = null;
    //    currentGameTask = null;

    //    gameTasksQueue.Clear();

    //    DoTask(masterAttackTask);        
    //}

    float repeatFrequency = 0.30f;

    IEnumerator FollowAttackTarget() {

        while(true) {
            yield return new WaitForSeconds(repeatFrequency);

            // If we still have a task we are navigating to, keep updating position
            if(currentGameTask != null && currentGameTask == targetTask && followingUnit!= null && navigatingToTask) {

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
