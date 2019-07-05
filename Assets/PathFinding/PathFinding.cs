using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;

public class PathFinding : MonoBehaviour {
    //public Transform seeker, target;

    PathfindingGrid grid;



    private void Awake() {
        grid = GetComponent<PathfindingGrid>();
    }

    public void FindSimplifiedPathToClosestGoal(Vector3 startPos, MineralType gatherGoal, Action<WorldPosition[], ActionableItem, bool> callback) {
        GameResourceManager resourceManager = Script.Get<GameResourceManager>();
        Ore[] allOreInGame = resourceManager.GetAllAvailableOfType(gatherGoal);

        int lowestLength = int.MaxValue;
        Node[] foundPath = null;
        ActionableItem foundObject = null;

        int completedCalls = 0;

        if (allOreInGame.Length == 0) {
            callback(null, null, false);
        }

        foreach(Ore ore in allOreInGame) {
            StartCoroutine(FindPath(startPos, ore.transform.position, (path, success) => {
                completedCalls++;

                if(success && path.Length < lowestLength) {
                    foundPath = path;
                    foundObject = ore;
                    lowestLength = path.Length;
                }

                if(completedCalls == allOreInGame.Length) {
                    bool anyPathSuccess = (foundPath != null && foundPath.Length > 0);

                    // Give the found object the flag that a task will soon be assiated
                    foundObject.taskAlreadyDictated = true;

                    callback(anyPathSuccess ? SimplifyPath(foundPath) : null, foundObject, anyPathSuccess);
                }
            }));
        }
    }

    //public static List<PathGridCoordinate> staticGridCoordinatesSurroundingLayoutCoordinate;

    // For PathRequestTargetType NodeCoordiante 
    public void FindSimplifiedPathForPathGrid(Vector3 startPos, LayoutCoordinate layoutCoordinate, Action<WorldPosition[], bool> callback) {
        Constants constants = Script.Get<Constants>();

        PathGridCoordinate[][] pathGridCoordinatesOfLayout = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);
        List<PathGridCoordinate> pathGridCoordinates = new List<PathGridCoordinate>();

        int middleCoordinate = Mathf.FloorToInt(constants.nodesPerLayoutPerAxis / 2f);        

        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[0][middleCoordinate]);
        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[middleCoordinate][0]);
        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[constants.nodesPerLayoutPerAxis - 1][middleCoordinate]);
        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[middleCoordinate][constants.nodesPerLayoutPerAxis - 1]);

        FindSimplifiedPathToAnyIncluding(pathGridCoordinates, startPos, callback);
    }

    // For PathRequestTargetType layout 
    public void FindSimplifiedPathForLayout(Vector3 startPos, LayoutCoordinate layoutCoordinate, Action<WorldPosition[], bool> callback) {
        Constants constants = Script.Get<Constants>();

        PathGridCoordinate[][] pathGridCoordinatesOfLayout = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);
        List<PathGridCoordinate> gridCoordinatesSurroundingLayoutCoordinate = new List<PathGridCoordinate>();

        int gridCoordinateCountWidth = constants.layoutMapWidth * constants.nodesPerLayoutPerAxis * constants.mapCountX;
        int gridCoordinateCountHeight = constants.layoutMapHeight * constants.nodesPerLayoutPerAxis * constants.mapCountY;

        PathGridCoordinate sample;

        int middleCoordinate = Mathf.FloorToInt(constants.nodesPerLayoutPerAxis / 2f);

        // Left Side
        sample = pathGridCoordinatesOfLayout[0][middleCoordinate];
        if(sample.xLowSample - 1 >= 0) {
            gridCoordinatesSurroundingLayoutCoordinate.Add(new PathGridCoordinate(sample.xLowSample - 1, sample.yLowSample)); 
        }

        // Top Side
        sample = pathGridCoordinatesOfLayout[middleCoordinate][0];
        if(sample.yLowSample - 1 >= 0) {
            gridCoordinatesSurroundingLayoutCoordinate.Add(new PathGridCoordinate(sample.xLowSample, sample.yLowSample - 1));            
        }

        // Right Side
        sample = pathGridCoordinatesOfLayout[constants.nodesPerLayoutPerAxis - 1][middleCoordinate];
        if(sample.xLowSample + 1 < gridCoordinateCountWidth - 1) {
            gridCoordinatesSurroundingLayoutCoordinate.Add(new PathGridCoordinate(sample.xLowSample + 1, sample.yLowSample));           
        }

        // Bottom Side
        sample = pathGridCoordinatesOfLayout[middleCoordinate][constants.nodesPerLayoutPerAxis - 1];
        if(sample.yLowSample + 1 < gridCoordinateCountHeight - 1) {
            gridCoordinatesSurroundingLayoutCoordinate.Add(new PathGridCoordinate(sample.xLowSample, sample.yLowSample + 1));
        }

        FindSimplifiedPathToAnyIncluding(gridCoordinatesSurroundingLayoutCoordinate, startPos, callback);
    }

    private void FindSimplifiedPathToAnyIncluding(List<PathGridCoordinate> pathGridCoordinates, Vector3 startPos, Action<WorldPosition[], bool> callback) {

        int lowestLength = int.MaxValue;
        Node[] foundPath = null;

        int completedCalls = 0;

        foreach(PathGridCoordinate gridCoordinate in pathGridCoordinates) {
            MapCoordinate mapCoordinate = MapCoordinate.FromGridCoordinate(gridCoordinate);
            WorldPosition worldPos = new WorldPosition(mapCoordinate);

            StartCoroutine(FindPath(startPos, worldPos.vector3, (path, success) => {
                completedCalls++;

                if(success && path.Length < lowestLength) {
                    foundPath = path;
                    lowestLength = path.Length;
                }

                if(completedCalls == pathGridCoordinates.Count) {
                    bool anyPathSuccess = (foundPath != null && foundPath.Length > 0);
                    callback(anyPathSuccess ? SimplifyPath(foundPath) : null, anyPathSuccess);
                }
            }));
        }
    }

    public void FindSimplifiedPath(Vector3 startPos, Vector3 targetPos, Action<WorldPosition[], bool> callback) {
        StartCoroutine(FindPath(startPos, targetPos, (path, success) => {
                callback((success && path.Length > 0) ? SimplifyPath(path) : null, success);          
        }));
    }

    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, Action<Node[], bool> callback){

        Stopwatch sw = new Stopwatch();
        sw.Start();

        Node startNode = grid.nodeFromWorldPoint(new WorldPosition(startPos));
        Node targetNode = grid.nodeFromWorldPoint(new WorldPosition(targetPos));

        Heap<Node> openSet = new Heap<Node>(grid.maxSize);
        HashSet<Node> closedSet = new HashSet<Node>();

        Node[] finalPath = new Node[0];
        bool success = false;

        // We need to reset the gCost and hCost, as well as the parent for this node to be fresh
        startNode.ResetPathfindingReferences();
        targetNode.ResetPathfindingReferences();

        openSet.Add(startNode);

        //Dictionary<(int, int), Node> nodeDictionary = new Dictionary<(int, int), Node>();

        while(openSet.Count > 0) {

            // Get node with lowest fcost, or if fCosts equal, lowest hCost
            Node currentNode = openSet.PopFirstItem();
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode) {
                sw.Stop();
                //print("Path Took " + sw.ElapsedMilliseconds + " Miliseconds");

                finalPath = RetracePath(targetNode).ToArray();
                success = true;
                break;
            }
            
            foreach(Node neighbour in grid.GetNeighbours(currentNode)) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + getDistance(currentNode, neighbour) + neighbour.movementPenalty;
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = getDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if(!openSet.Contains(neighbour)) {
                        openSet.Add(neighbour);

                        //(int, int) key = (neighbour.gridX, neighbour.gridY);
                        //if (nodeDictionary.ContainsKey(key)) {
                        //    print("What the fuck");
                        //} else {
                        //    nodeDictionary.Add(key, neighbour);
                        //}

                    } else {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
        }

        callback(finalPath, success);
        yield return null;
    }

    List<Node> RetracePath(Node fromNode) {
        List<Node> path = new List<Node>();
        Node currentNode = fromNode;
        
        while (currentNode != null) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        return path;
    }

    WorldPosition[] SimplifyPath(Node[] path) {
        List<WorldPosition> waypoints = new List<WorldPosition>();
        Vector2 oldDirection = Vector2.zero;

        int lastAddedIndex = 0;

        for (int i = 1; i < path.Length; i++) {
            Vector2 newDirection = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if(newDirection != oldDirection) {

                if (lastAddedIndex != i - 1) {
                    WorldPosition oldPathPosition = path[i - 1].worldPosition;
                    oldPathPosition.recalculateHeight();

                    waypoints.Add(oldPathPosition);
                }                

                WorldPosition pathPosition = path[i].worldPosition;
                pathPosition.recalculateHeight();

                waypoints.Add(pathPosition);

                lastAddedIndex = i;
                oldDirection = newDirection;
            }
        }

        WorldPosition finalPathPosition = path[path.Length - 1].worldPosition;
        finalPathPosition.recalculateHeight();

        waypoints.Add(finalPathPosition);

        return waypoints.ToArray();
    }

    int getDistance(Node nodeA, Node nodeB) {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        
        if (distX > distY) {
            return 14 * distY + 10 * (distX - distY);
        }

        return 14 * distX + 10 * (distY - distX);
    }

}
