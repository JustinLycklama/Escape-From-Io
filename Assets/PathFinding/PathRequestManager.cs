using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathRequestTargetType { World, Layout }

struct PathRequest {
    public PathRequestTargetType targetType;
    
    public Vector3? pathEndWorld;
    public PathGridCoordinate? pathEndGrid;
    public LayoutCoordinate? pathEndLayout;

    public Vector3 pathStart;

    public Action<WorldPosition[], bool> callback;

    public PathRequest(Vector3 _start, Vector3 _end, Action<WorldPosition[], bool> _callback) {
        pathStart = _start;
        pathEndWorld = _end;
        callback = _callback;

        targetType = PathRequestTargetType.World;
        pathEndGrid = null;
        pathEndLayout = null;
    }

    //public PathRequest(Vector3 _start, PathGridCoordinate _end, Action<WorldPosition[], bool> _callback) {
    //    pathStart = _start;
    //    pathEndGrid = _end;
    //    callback = _callback;

    //    targetType = PathRequestTargetType.World;
    //    pathEndWorld = null;
    //    pathEndLayout = null;
    //}

    public PathRequest(Vector3 _start, LayoutCoordinate _end, Action<WorldPosition[], bool> _callback) {
        pathStart = _start;
        pathEndLayout = _end;
        callback = _callback;

        targetType = PathRequestTargetType.Layout;
        pathEndWorld = null;
        pathEndGrid = null;
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

    public static void RequestPathForTask(Vector3 position, GameTask task, System.Action<WorldPosition[], bool> callback) {

        PathRequest? request = null;

        switch(task.targetType) {
            case PathRequestTargetType.World:
                request = new PathRequest(position, task.target.vector3, callback);
                break;
            case PathRequestTargetType.Layout:
                MapCoordinate mapCoordinate = new MapCoordinate(task.target);
                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
                request = new PathRequest(position, layoutCoordinate, callback);
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

            if (currentPathRequest.targetType == PathRequestTargetType.World) {
                pathFinding.FindSimplifiedPath(currentPathRequest.pathStart, currentPathRequest.pathEndWorld.Value, (path, success) => {
                    currentPathRequest.callback(path, success);
                    isProcessingPath = false;

                    TryProcessNext();
                });
            } else if(currentPathRequest.targetType == PathRequestTargetType.Layout) {

                pathFinding.FindSimplifiedPathToAnySurrounding(currentPathRequest.pathStart, currentPathRequest.pathEndLayout.Value, (path, success) => {
                    currentPathRequest.callback(path, success);
                    isProcessingPath = false;

                    TryProcessNext();
                });
            }           
        }
    }
}
