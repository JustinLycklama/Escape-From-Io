using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathRequestTargetType { Unknown, World, Layout, PathGrid }

public class PathRequest {
    public bool isCancelled = false;

    public PathRequestTargetType targetType;
    
    public Vector3? pathEndWorld;
    public PathGridCoordinate? pathEndGrid;
    public LayoutCoordinate? pathEndLayout;

    // If we are attempting to path to an object at distance, it means we want to move close to but not beside the object
    public bool pathToObjectAtDistance = false;

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

    public PathRequest(Vector3 _start, PathGridCoordinate _end, int _movementPenaltyMultiplier, Action<LookPoint[], ActionableItem, bool, int> _callback, PathRequestTargetType targetType, bool atDistance = false) {
        pathStart = _start;
        callback = _callback;
        pathEndGrid = _end;
        movementPenaltyMultiplier = _movementPenaltyMultiplier;

        pathToObjectAtDistance = atDistance;

        this.targetType = targetType;
        pathEndWorld = null;
        pathEndLayout = null;
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

    // Set once the request's callback has been delivered (or skipped because cancelled). Guards against double completion.
    public bool isCompleted = false;

    public void Cancel() {
        isCancelled = true;
    }
}

public class PathRequestManager : MonoBehaviour {

    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathRequestManager instance;
    PathFinding pathFinding;

    bool isProcessingPath;
    bool isDraining;
    float processingStartedAt;

    // One-line state dump for watchdog logs and debugging
    public static string DiagnosticSummary() {
        if (instance == null) { return "[PathRequests: no instance]"; }
        string busyFor = instance.isProcessingPath ? $" for {Time.realtimeSinceStartup - instance.processingStartedAt:F1}s" : "";
        return $"[PathRequests busy: {instance.isProcessingPath}{busyFor}, queued: {instance.pathRequestQueue.Count}]";
    }

    // Safety net: searches complete synchronously, so being busy for long means a search never called back.
    // Fails the stuck request and resumes the queue. Returns true if a reset happened.
    public static bool ResetIfStuck(float maxSeconds) {
        if (instance == null || !instance.isProcessingPath) { return false; }
        if (Time.realtimeSinceStartup - instance.processingStartedAt < maxSeconds) { return false; }

        Debug.LogError($"Path request stuck for >{maxSeconds}s (target type {instance.currentPathRequest?.targetType}); failing it and resuming. {DiagnosticSummary()}");
        instance.Complete(instance.currentPathRequest, null, null, false, 0);
        return true;
    }

    private void Awake() {
        instance = this;
        pathFinding = GetComponent<PathFinding>();
    }

    public static PathRequest RequestPathForTask(Vector3 position, int movementPenaltyMultiplier, GameTask task, Action<LookPoint[], ActionableItem, bool, int> callback) {

        PathRequest request = null;

        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(task.target);
        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
        PathGridCoordinate pathGridCoordinate = PathGridCoordinate.fromMapCoordinate(mapCoordinate);

        switch(task.pathRequestTargetType) {
            case PathRequestTargetType.World:
                request = new PathRequest(position, task.target.vector3, movementPenaltyMultiplier, callback);
                break;
            case PathRequestTargetType.Layout:
                request = new PathRequest(position, layoutCoordinate, movementPenaltyMultiplier, callback, task.pathRequestTargetType);
                break;
            case PathRequestTargetType.PathGrid:
                request = new PathRequest(position, pathGridCoordinate, movementPenaltyMultiplier, callback, task.pathRequestTargetType, task.action == GameTask.ActionType.AttackRanged);
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

        if (request != null) {
            RequestPath(request);
        }

        return request;
    }

    private static void RequestPath(PathRequest pathRequest) {
        instance.pathRequestQueue.Enqueue(pathRequest);

        instance.TryProcessNext();
    }

    // Processes queued requests one at a time. Searches usually complete synchronously, so this drains in a loop
    // instead of recursing through each completion callback.
    void TryProcessNext() {
        if (isDraining) { return; }
        isDraining = true;

        try {
            while (!isProcessingPath && pathRequestQueue.Count > 0) {
                PathRequest request = pathRequestQueue.Dequeue();
                if (request.isCancelled) {
                    request.isCompleted = true;
                    continue;
                }

                currentPathRequest = request;
                isProcessingPath = true;
                processingStartedAt = Time.realtimeSinceStartup;

                try {
                    StartSearch(request);
                } catch (Exception ex) {
                    // e.g. a destroyed target object; treat as "no path" so the caller still hears back
                    Debug.LogException(ex);
                    Complete(request, null, null, false, 0);
                }
            }
        } finally {
            isDraining = false;
        }
    }

    // Delivers a result exactly once, and always frees the queue, even if the caller's callback throws.
    void Complete(PathRequest request, LookPoint[] path, ActionableItem item, bool success, int distance) {
        if (request == null || request.isCompleted) { return; }
        request.isCompleted = true;

        try {
            if (!request.isCancelled) {
                request.callback(path, item, success, distance);
            }
        } catch (Exception ex) {
            Debug.LogException(ex);
        } finally {
            if (request == currentPathRequest) {
                currentPathRequest = null;
                isProcessingPath = false;
            }
        }

        TryProcessNext();
    }

    void StartSearch(PathRequest request) {
        switch(request.targetType) {
            case PathRequestTargetType.Unknown:
                if (request.pathEndGoalMineral != null) {
                    pathFinding.FindSimplifiedPathToClosestOre(request.pathStart, request.movementPenaltyMultiplier, request.pathEndGoalMineral.Value,
                        (path, actionableItem, success, distance) => Complete(request, path, actionableItem, success, distance));
                } else if (request.pathEndGoalFaction != null) {
                    pathFinding.FindSimplifiedPathToClosestUnit(request.pathStart, request.movementPenaltyMultiplier, request.pathEndGoalFaction.Value,
                        (path, actionableItem, success, distance) => Complete(request, path, actionableItem, success, distance));
                } else {
                    Complete(request, null, null, false, 0);
                }
                break;
            case PathRequestTargetType.World:
                pathFinding.FindSimplifiedPath(request.pathStart, request.pathEndWorld.Value, request.movementPenaltyMultiplier,
                    (path, success, distance) => Complete(request, path, null, success, distance));
                break;
            case PathRequestTargetType.Layout:
                pathFinding.FindSimplifiedPathForLayout(request.pathStart, request.pathEndLayout.Value, request.movementPenaltyMultiplier,
                    (path, success, distance) => Complete(request, path, null, success, distance));
                break;
            case PathRequestTargetType.PathGrid:
                if (request.pathToObjectAtDistance) {
                    pathFinding.FindSimplifiedPathForPathGridAtDistanceOne(request.pathStart, request.pathEndGrid.Value, request.movementPenaltyMultiplier,
                        (path, success, distance) => Complete(request, path, null, success, distance));
                } else {
                    pathFinding.FindSimplifiedPathForPathGrid(request.pathStart, request.pathEndGrid.Value, request.movementPenaltyMultiplier,
                        (path, success, distance) => Complete(request, path, null, success, distance));
                }
                break;
            default:
                Complete(request, null, null, false, 0);
                break;
        }
    }
}
