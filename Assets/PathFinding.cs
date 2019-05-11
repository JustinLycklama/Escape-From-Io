using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;

public class PathFinding : MonoBehaviour {
    //public Transform seeker, target;

    Grid grid;

    private void Awake() {
        grid = GetComponent<Grid>();
    }

    //private void Update() {
    //    if (Input.GetButtonDown("Jump")) {
    //        FindPath(seeker.position, target.position);
    //    }
    //}

    public void FindSimplifiedPath(Vector3 startPos, Vector3 targetPos, Action<Vector3[], bool> callback) {
        StartCoroutine(FindPath(startPos, targetPos, (path, success) => {
            callback(SimplifyPath(path), success);
        }));
    }

    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, Action<Node[], bool> callback){

        Stopwatch sw = new Stopwatch();
        sw.Start();

        Node startNode = grid.nodeFromWorldPoint(startPos);
        Node targetNode = grid.nodeFromWorldPoint(targetPos);

        Heap<Node> openSet = new Heap<Node>(grid.maxSize);
        HashSet<Node> closedSet = new HashSet<Node>();

        Node[] finalPath = new Node[0];
        bool success = false;

        openSet.Add(startNode);

        while(openSet.Count > 0) {
            // Get node with lowest fcost, or if fCosts equal, lowest hCost
            Node currentNode = openSet.PopFirstItem();
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode) {
                sw.Stop();
                print("Path Took " + sw.ElapsedMilliseconds);

                finalPath = RetracePath(targetNode).ToArray();
                success = true;
                break;
            }
            
            foreach(Node neighbour in grid.GetNeighbours(currentNode)) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + getDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = getDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;
                }
                
                if (!openSet.Contains(neighbour)) {
                    openSet.Add(neighbour);
                }
            }
        }

        callback(finalPath, success);
        yield return null;
    }

    List<Node> RetracePath(Node fromNode) {
        List<Node> path = new List<Node>();
        Node currentNode = fromNode;
        
        while (currentNode.parent != null) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        return path;
    }

    Vector3[] SimplifyPath(Node[] path) {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 oldDirection = Vector2.zero;

        for (int i = 1; i < path.Length; i++) {
            Vector2 newDirection = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if (newDirection != oldDirection) {
                waypoints.Add(path[i].worldPosition);
                oldDirection = newDirection;
            }
        }

        waypoints.Add(path[path.Length - 1].worldPosition);

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
