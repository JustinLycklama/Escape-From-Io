using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : HeapItem<Node> {
    public bool walkable;
    public Vector3 worldPosition;

    public Node parent;
    
    public int gridX, gridY;
    
    public int gCost;
    public int hCost;
    
    public int fCost {
        get {
            return gCost + hCost;
        }
    }

    public int heapIndex;
    int HeapItem<Node>.heapIndex { get => this.heapIndex; set => this.heapIndex = value; }

    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY) {
        walkable = _walkable;
        worldPosition = _worldPos;

        gridX = _gridX;
        gridY = _gridY;
    }

    // HeapItem Interface
    public int CompareTo(Node other) {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0) {
            compare = hCost.CompareTo(other.hCost);
        }

        return -compare;
    }
}
