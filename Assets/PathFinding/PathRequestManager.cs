using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathRequestTargetType { Unknown, World, Layout, PathGrid }

struct PathRequest {
    public PathRequestTargetType targetType;
    
    public Vector3? pathEndWorld;

    public PathGridCoordinate? pathEndGrid;
    public LayoutCoordinate? pathEndLayout;
    public MineralType? pathEndGoalMineral;
    public Unit.FactionType? pathEndGoalFaction;

    public Vector3 pathStart;

    public int movementPenaltyMultiplier;

    public Action<LookPoint[], ActionableItem, bool, int> callback;

    public PathRequest(Vector3 _start, Vector3 _end, int _movementPenaltyMultiplier, Action<LookPoint[], ActionableItem, bool, int> _callback) {
        pathStart = _start;
        pathEndWorld = _end;
        callback = _callback;
        movementPenaltyMultiplier = _movementPenaltyMultiplier;

        targetType = PathRequestTargetType.World;
        pathEndGrid = null;
        pathEndLayout = null;
        pathEndGoalMineral = null;
        pathEndGoalFaction = null;
    }

    //public PathRequest(Vector3 _start, PathGridCoordinate _end, Action<WorldPosition[], bool> _callback) {
    //    pathStart = _start;
    //    pathEndGrid = _end;
    //    callback = _callback;

    //    targetType = PathRequestTargetType.World;
    //    pathEndWorld = null;
    //    pathEndLayout = null;
    //}

    public PathRequest(Vector3 _start, LayoutCoordinate _end, int _movementPenaltyMultiplier, Action<LookPoint[], ActionableItem, bool, int> _callback, PathRequestTargetType targetType) {
        pathStart = _start;
        pathEndLayout = _end;
        callback = _callback;
        movementPenaltyMultiplier = _movementPenaltyMultiplier;


        this.targetType = targetType;
        pathEndWorld = null;
        pathEndGrid = null;
        pathEndGoalMineral = null;
        pathEndGoalFaction = null;
    }

    public PathRequest(Vector3 _start, int _movementPenaltyMultiplier, MineralType goalGatherType, Action<LookPoint[], ActionableItem, bool, int> _callback) {
        pathStart = _start;
        pathEndGoalMineral = goalGatherType;
        callback = _callback;
        movementPenaltyMultiplier = _movementPenaltyMultiplier;

        targetType = PathRequestTargetType.Unknown;
        pathEndWorld = null;
        pathEndGrid = null;
        pathEndLayout = null;
        pathEndGoalFaction = null;
    }

    public PathRequest(Vector3 _start, int _movementPenaltyMultiplier, Unit.FactionType attackTarget, Action<LookPoint[], ActionableItem, bool, int> _callback) {
        pathStart = _start;
        pathEndGoalFaction = attackTarget;
        callback = _callback;
        movementPenaltyMultiplier = _movementPenaltyMultiplier;

        targetType = PathRequestTargetType.Unknown;
        pathEndWorld = null;
        pathEndGrid = null;
        pathEndLayout = null;
        pathEndGoalMineral = null;
    }
}

public class PathRequestManager : MonoBehaviour {

    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathRequestManager instance;
    PathFinding pathFinding;

    bool isProcessingPath;

    private void Awake() {
        instance = this;
        pathFinding = GetComponent<PathFinding>();
    }

    //public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<WorldPosition[], bool> callback, GameTask.TargetType targetType = GameTask.TargetType.World) {
    //    PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback, targetType);
    //    instance.pathRequestQueue.Enqueue(newRequest);

    //    instance.TryProcessNext();
    //}

    public static void RequestPathForTask(Vector3 position, int movementPenaltyMultiplier, GameTask task, Action<LookPoint[], ActionableItem, bool, int> callback) {

        PathRequest? request = null;

        switch(task.pathRequestTargetType) {
            case PathRequestTargetType.World:
                request = new PathRequest(position, task.target.vector3, movementPenaltyMultiplier, callback);
                break;
            case PathRequestTargetType.Layout:
            case PathRequestTargetType.PathGrid:
                MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(task.target);
                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
                request = new PathRequest(position, layoutCoordinate, movementPenaltyMultiplier, callback, task.pathRequestTargetType);
                break;
            case PathRequestTargetType.Unknown:
                if (task.gatherType != null) {
                    var gatherType = task.gatherType.Value;
                    request = new PathRequest(position, movementPenaltyMultiplier, gatherType, callback);
                } else if (task.attackTarget != null) {
                    var attackTarget = task.attackTarget.Value;
                    request = new PathRequest(position, movementPenaltyMultiplier, attackTarget, callback);
                }

                break;
        }

        if (request.HasValue) {
            RequestPath(request.Value);
        }
    }

    private static void RequestPath(PathRequest pathRequest) {
        instance.pathRequestQueue.Enqueue(pathRequest);

        instance.TryProcessNext();
    }

    void TryProcessNext() {
        if (!isProcessingPath && pathRequestQueue.Count > 0) {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;

            switch(currentPathRequest.targetType) {
                case PathRequestTargetType.Unknown:
                    if (currentPathRequest.pathEndGoalMineral != null) {
                        pathFinding.FindSimplifiedPathToClosestOre(currentPathRequest.pathStart, currentPathRequest.movementPenaltyMultiplier, currentPathRequest.pathEndGoalMineral.Value, (path, actionableItem, success, distance) => {
                            currentPathRequest.callback(path, actionableItem, success, distance);
                            isProcessingPath = false;

                            TryProcessNext();
                        });
                    } else if (currentPathRequest.pathEndGoalFaction != null) {
                        pathFinding.FindSimplifiedPathToClosestUnit(currentPathRequest.pathStart, currentPathRequest.movementPenaltyMultiplier, currentPathRequest.pathEndGoalFaction.Value, (path, actionableItem, success, distance) => {
                            currentPathRequest.callback(path, actionableItem, success, distance);
                            isProcessingPath = false;

                            TryProcessNext();
                        });
                    }

                    break;
                case PathRequestTargetType.World:
                    pathFinding.FindSimplifiedPath(currentPathRequest.pathStart, currentPathRequest.pathEndWorld.Value, currentPathRequest.movementPenaltyMultiplier,(path, success, distance) => {
                        currentPathRequest.callback(path, null, success, distance);
                        isProcessingPath = false;

                        TryProcessNext();
                    });


                    break;
                case PathRequestTargetType.Layout:
                    pathFinding.FindSimplifiedPathForLayout(currentPathRequest.pathStart, currentPathRequest.pathEndLayout.Value, currentPathRequest.movementPenaltyMultiplier, (path, success, distance) => {
                        currentPathRequest.callback(path, null, success, distance);
                        isProcessingPath = false;

                        TryProcessNext();
                    });
                    break;
                case PathRequestTargetType.PathGrid:
                    pathFinding.FindSimplifiedPathForPathGrid(currentPathRequest.pathStart, currentPathRequest.pathEndLayout.Value, currentPathRequest.movementPenaltyMultiplier, (path, success, distance) => {
                        currentPathRequest.callback(path, null, success, distance);
                        isProcessingPath = false;

                        TryProcessNext();
                    });
                    break;
            }
        }
    }
}
