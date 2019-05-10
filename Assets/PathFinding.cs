using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;

public class PathFinding : MonoBehaviour {
    public Transform seeker, target;

    Grid grid;

    private void Awake() {
        grid = GetComponent<Grid>();
    }

    private void Update() {
        if (Input.GetButtonDown("Jump")) {
            FindPath(seeker.position, target.position);
        }
    }

    void FindPath(Vector3 startPos, Vector3 targetPos){

        Stopwatch sw = new Stopwatch();
        sw.Start();

        Node startNode = grid.nodeFromWorldPoint(startPos);
        Node targetNode = grid.nodeFromWorldPoint(targetPos);

        Heap<Node> openSet = new Heap<Node>(grid.maxSize);
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while(openSet.Count > 0) {
            // Get node with lowest fcost, or if fCosts equal, lowest hCost
            Node currentNode = openSet.PopFirstItem();
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode) {
                sw.Stop();
                print("Path Took " + sw.ElapsedMilliseconds);

                RetracePath(targetNode);
                return;
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
    }
    
    void RetracePath(Node fromNode) {
        List<Node> path = new List<Node>();
        Node currentNode = fromNode;
        
        while (currentNode.parent != null) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        grid.path = path;
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
