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

    public void FindSimplifiedPathToClosestUnit(Vector3 startPos, int movementPenaltyMultiplier, Unit.FactionType attackTarget, Action<LookPoint[], ActionableItem, bool, int> callback) {
        UnitManager unitManager = Script.Get<UnitManager>();
        Unit[] allUnits = unitManager.GetAllPlayerUnits();

        FindSimplifiedPathToClosestGoal(startPos, movementPenaltyMultiplier, allUnits, callback);
    }

    public void FindSimplifiedPathToClosestOre(Vector3 startPos, int movementPenaltyMultiplier, MineralType gatherGoal, Action<LookPoint[], ActionableItem, bool, int> callback) {
        GameResourceManager resourceManager = Script.Get<GameResourceManager>();
        Ore[] allOreInGame = resourceManager.GetAllAvailableOfType(gatherGoal);

        FindSimplifiedPathToClosestGoal(startPos, movementPenaltyMultiplier, allOreInGame, callback);
    }

    // Since this is associating an object to a future task, this cannot be done just as a check, must be done when unit is actually moving to task
    public void FindSimplifiedPathToClosestGoal(Vector3 startPos, int movementPenaltyMultiplier, ActionableItem[] objectList, Action<LookPoint[], ActionableItem, bool, int> callback) {

        int lowestLength = int.MaxValue;
        Node[] foundPath = null;
        ActionableItem foundObject = null;

        int completedCalls = 0;

        if(objectList.Length == 0) {
            callback(null, null, false, 0);
        }

        foreach(ActionableItem obj in objectList) {
            StartCoroutine(FindPath(startPos, obj.transform.position, movementPenaltyMultiplier, (path, success) => {
                completedCalls++;

                if(success && path.Length < lowestLength) {
                    foundPath = path;
                    foundObject = obj;
                    lowestLength = path.Length;
                }

                if(completedCalls == objectList.Length) {
                    bool anyPathSuccess = (foundPath != null && foundPath.Length > 0);

                    // Give the found object the flag that a task will soon be associated
                    if (foundObject != null) {
                        foundObject.taskAlreadyDictated = true;
                    }                    

                    int totalDistance = 0;
                    callback(anyPathSuccess ? SimplifyPath(foundPath, movementPenaltyMultiplier, out totalDistance) : null, foundObject, anyPathSuccess, totalDistance);
                }
            }));
        }
    }

    //public static List<PathGridCoordinate> staticGridCoordinatesSurroundingLayoutCoordinate;

    // For PathRequestTargetType NodeCoordiante 
    public void FindSimplifiedPathForPathGrid(Vector3 startPos, LayoutCoordinate layoutCoordinate, int movementPenaltyMultiplier, Action<LookPoint[], bool, int> callback) {
        Constants constants = Script.Get<Constants>();

        PathGridCoordinate[][] pathGridCoordinatesOfLayout = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);
        List<PathGridCoordinate> pathGridCoordinates = new List<PathGridCoordinate>();

        int middleCoordinate = Mathf.FloorToInt(constants.nodesPerLayoutPerAxis / 2f);        

        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[0][middleCoordinate]);
        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[middleCoordinate][0]);
        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[constants.nodesPerLayoutPerAxis - 1][middleCoordinate]);
        pathGridCoordinates.Add(pathGridCoordinatesOfLayout[middleCoordinate][constants.nodesPerLayoutPerAxis - 1]);

        FindSimplifiedPathToAnyIncluding(pathGridCoordinates, startPos, movementPenaltyMultiplier, callback);
    }

    // For PathRequestTargetType layout 
    public void FindSimplifiedPathForLayout(Vector3 startPos, LayoutCoordinate layoutCoordinate, int movementPenaltyMultiplier, Action<LookPoint[], bool, int> callback) {
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

        FindSimplifiedPathToAnyIncluding(gridCoordinatesSurroundingLayoutCoordinate, startPos, movementPenaltyMultiplier, callback);
    }

    private void FindSimplifiedPathToAnyIncluding(List<PathGridCoordinate> pathGridCoordinates, Vector3 startPos, int movementPenaltyMultiplier, Action<LookPoint[], bool, int> callback) {

        int lowestLength = int.MaxValue;
        Node[] foundPath = null;

        int completedCalls = 0;

        foreach(PathGridCoordinate gridCoordinate in pathGridCoordinates) {
            MapCoordinate mapCoordinate = MapCoordinate.FromGridCoordinate(gridCoordinate);
            WorldPosition worldPos = new WorldPosition(mapCoordinate);

            StartCoroutine(FindPath(startPos, worldPos.vector3, movementPenaltyMultiplier, (path, success) => {
                completedCalls++;

                if(success && path.Length < lowestLength) {
                    foundPath = path;
                    lowestLength = path.Length;
                }

                if(completedCalls == pathGridCoordinates.Count) {
                    bool anyPathSuccess = (foundPath != null && foundPath.Length > 0);
                    int totalDistance = 0;
                    callback(anyPathSuccess ? SimplifyPath(foundPath, movementPenaltyMultiplier, out totalDistance) : null, anyPathSuccess, totalDistance);
                }
            }));
        }
    }

    public void FindSimplifiedPath(Vector3 startPos, Vector3 targetPos, int movementPenaltyMultiplier, Action<LookPoint[], bool, int> callback) {
        StartCoroutine(FindPath(startPos, targetPos, movementPenaltyMultiplier, (path, success) => {
            int totalDistance = 0;
            callback((success && path.Length > 0) ? SimplifyPath(path, movementPenaltyMultiplier, out totalDistance) : null, success, totalDistance);          
        }));
    }

    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, int movementPenaltyMultiplier, Action<Node[], bool> callback){

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

                int newMovementCostToNeighbour = currentNode.gCost + getDistance(currentNode, neighbour) + (neighbour.movementPenalty * movementPenaltyMultiplier);
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

    LookPoint[] SimplifyPath(Node[] path, int movementPenaltyMultiplier, out int totalDistance) {
        List<LookPoint> lookpoints = new List<LookPoint>();
        Vector2 oldDirection = Vector2.zero;

        int lastAddedIndex = 0;
        int distance = 0;

        int movementPointsToThisWaypoint = 0;

        for (int i = 1; i < path.Length; i++) {
            Node previousNode = path[i - 1];
            Node currentNode = path[i];

            int movementPointsToThisNode = getDistance(previousNode, currentNode) + currentNode.movementPenalty * movementPenaltyMultiplier;

            movementPointsToThisWaypoint += movementPointsToThisNode;
            distance += movementPointsToThisNode;

            Vector2 newDirection = new Vector2(previousNode.gridX - currentNode.gridX, previousNode.gridY - currentNode.gridY);
            if(newDirection != oldDirection) {

                if(lastAddedIndex != i - 1) {
                    WorldPosition oldPathPosition = previousNode.worldPosition;
                    oldPathPosition.recalculateHeight();

                    // If we are adding a previous look point for the sake of not hitting object corners, lets say the "entire distance" 
                    // is covered by this previous point. We are only estimating after all
                    lookpoints.Add(new LookPoint(oldPathPosition, movementPointsToThisWaypoint));
                    movementPointsToThisWaypoint = 0;
                }

                WorldPosition pathPosition = currentNode.worldPosition;
                pathPosition.recalculateHeight();

                lookpoints.Add(new LookPoint(pathPosition, movementPointsToThisWaypoint));

                lastAddedIndex = i;
                oldDirection = newDirection;

                movementPointsToThisWaypoint = 0;
            }
        }

        totalDistance = distance;

        Node lastNode = path[path.Length - 1];        

        WorldPosition finalPathPosition = lastNode.worldPosition;
        finalPathPosition.recalculateHeight();

        lookpoints.Add(new LookPoint(finalPathPosition, movementPointsToThisWaypoint));

        // Find out how much of the journey each waypoint takes
        for(int i = 0; i < lookpoints.Count; i++) {
            LookPoint currentPoint = lookpoints[i];
            currentPoint.percentOfJourney = (float)currentPoint.movementPoints / (float)totalDistance;
        }

        return lookpoints.ToArray();
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
