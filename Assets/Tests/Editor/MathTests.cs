using NUnit.Framework;
using UnityEngine;

public class HeapTests {

    // Mirrors Node's convention: CompareTo returns > 0 for the higher-priority (lower cost) item
    class Item : HeapItem<Item> {
        public int cost;
        public Item(int cost) { this.cost = cost; }
        public int heapIndex { get; set; }
        public string description => cost.ToString();
        public void ResetPathfindingReferences() { }
        public int CompareTo(Item other) { return -cost.CompareTo(other.cost); }
    }

    [Test]
    public void PopsLowestCostFirst() {
        var heap = new Heap<Item>(16);
        foreach (int c in new[] { 7, 3, 9, 1, 5, 8, 2 }) heap.Add(new Item(c));

        var popped = new System.Collections.Generic.List<int>();
        while (heap.Count > 0) popped.Add(heap.PopFirstItem().cost);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 5, 7, 8, 9 }, popped);
    }

    [Test]
    public void UpdateItemReordersAfterCostDecrease() {
        var heap = new Heap<Item>(8);
        var a = new Item(5); var b = new Item(6); var c = new Item(7);
        heap.Add(a); heap.Add(b); heap.Add(c);

        c.cost = 1;
        heap.UpdateItem(c);

        Assert.AreSame(c, heap.PopFirstItem());
    }

    [Test]
    public void ContainsTracksMembership() {
        var heap = new Heap<Item>(4);
        var a = new Item(1); var b = new Item(2);
        heap.Add(a);

        Assert.IsTrue(heap.Contains(a));
        Assert.IsFalse(heap.Contains(b));
    }
}

public class LineTests {

    [Test]
    public void DetectsCrossingAVerticalLine() {
        // Line through the origin, perpendicular to the approach from the left: the line x = 0
        var line = new Line(Vector2.zero, new Vector2(-1, 0));

        Assert.IsFalse(line.HasCrossedLine(new Vector2(-0.5f, 3)));
        Assert.IsTrue(line.HasCrossedLine(new Vector2(0.5f, -3)));
    }

    [Test]
    public void DetectsCrossingADiagonalLine() {
        // Approaching (1,1) from the origin; the line is perpendicular to that direction
        var line = new Line(new Vector2(1, 1), Vector2.zero);

        Assert.IsFalse(line.HasCrossedLine(new Vector2(0.5f, 0.5f)));
        Assert.IsTrue(line.HasCrossedLine(new Vector2(2, 2)));
    }

    [Test]
    public void DistanceFromPointIsPerpendicularDistance() {
        var line = new Line(Vector2.zero, new Vector2(-1, 0));

        Assert.AreEqual(2f, line.DistanceFromPoint(new Vector2(2, 5)), 0.01f);
    }
}
