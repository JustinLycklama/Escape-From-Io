using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface HeapItem<T> : IComparable<T> {
    int heapIndex {
        get;
        set;
    }

    string description {
        get;
    }
}

public class Heap<T> : MonoBehaviour where T: class, HeapItem<T>
{
    T[] items;
    int currentItemCount;

    public Heap(int maxHeapSize) {
        items = new T[maxHeapSize];
    }

    public void Add(T item) {
        item.heapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T PopFirstItem() {
        if (currentItemCount == 0) {
            return null;
        }

        T item = items[0];
        currentItemCount--;

        items[0] = items[currentItemCount];
        items[0].heapIndex = 0;

        SortDown(items[0]);
        return item;
    }

    public int Count {
        get {
            return currentItemCount;
        }
    }

    public void UpdateItem(T item) {
        SortUp(item);
    }

    public bool Contains(T item) {
        return Equals(items[item.heapIndex], item);
    }

    void SortDown(T item) {
        while(true) {
            T left = GetLeftChild(item);
            T right = GetRightChild(item);
            
            if (left != null) {
                T compareItem = left;

                // If right value exists and is less than left
                if (right != null && right.CompareTo(left) > 0) {
                    compareItem = right;
                }

                if (item.CompareTo(compareItem) < 0) {
                    Swap(item, compareItem);
                } else {
                    // Item is smaller than both children
                    return;
                }

            } else {
                // Item has no children
                return;
            }
        }
    }

    void SortUp(T item) {

        T currentItem = item;
        T parentItem = GetParent(currentItem);

        while(parentItem != null) {
            if (currentItem.CompareTo(parentItem) > 0) {
                Swap(currentItem, parentItem);
            } else {
                break; 
            }

            parentItem = GetParent(currentItem);
        }
    }

    void Swap(T itemA, T itemB) {
        items[itemA.heapIndex] = itemB;
        items[itemB.heapIndex] = itemA;

        int tempIndex = itemA.heapIndex;
        itemA.heapIndex = itemB.heapIndex;
        itemB.heapIndex = tempIndex;
    }

    public void description() {
        recurseDescription( new[] { items[0] } );
    }

    private void recurseDescription(T[] parents) {

        string currentParentDescriptions = "";
        List<T> newChildren = new List<T>();

        foreach (T item in parents) {
            currentParentDescriptions += item.description + " | ";

            T left = GetLeftChild(item);
            T right = GetRightChild(item);

            if (left != null) {
                newChildren.Add(left);
            }

            if (right != null) {
                newChildren.Add(right);
            }
        }
        
        print(currentParentDescriptions);

        if(newChildren.Count > 0) {
            recurseDescription(newChildren.ToArray());
        }
    }

    // Convenience item methods
    T GetParent(T item) {
        if (item.heapIndex == 0) {
            return null;
        }

        int parentIndex = (item.heapIndex - 1) / 2;
        return items[parentIndex];
    } 

    T GetLeftChild(T item) {
        int childIndex = (item.heapIndex * 2) + 1;

        if (childIndex >= currentItemCount) {
            return null;
        }

        return items[childIndex];
    }

    T GetRightChild(T item) {
        int childIndex = (item.heapIndex * 2) + 2;

        if(childIndex >= currentItemCount) {
            return null;
        }

        return items[childIndex];
    }
}

