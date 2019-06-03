using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : HeapItem<Node> {
    public bool walkable;
    public int movementPenalty;

    public readonly int gridX, gridY;
    public readonly WorldPosition worldPosition;

    // Pathfinding Pass References
    // Should not be populated outside of a path finding run.
    public Node parent;
    
    public int gCost;
    public int hCost;

    public int fCost {
        get {
            return gCost + hCost;
        }
    }

    public int heapIndex;
    int HeapItem<Node>.heapIndex { get => this.heapIndex; set => this.heapIndex = value; }

    public Node(bool _walkable, WorldPosition _worldPos, int _gridX, int _gridY, int _penalty) {
        walkable = _walkable;
        worldPosition = _worldPos;

        gridX = _gridX;
        gridY = _gridY;

        movementPenalty = _penalty;
    }

    // HeapItem Interface
    public int CompareTo(Node other) {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0) {
            compare = hCost.CompareTo(other.hCost);
        }

        return -compare;
    }

    public void ResetPathfindingReferences() {
        parent = null;
        gCost = int.MaxValue;
        hCost = int.MaxValue;

        heapIndex = 0;
    }

    public string description {
        get {
            return fCost + "";
        }
    }
}
