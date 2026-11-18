using NUnit.Framework;
using UnityEngine;

// MeshGenerator.CalculateNormals (#25): terrain normals from the global height map, creased at cliff bases.
public class TerrainNormalTests {

    static readonly Vector3 scale = new Vector3(10, 100, 10);

    // 8 samples wide, 3 tall: flat ground (0.158) for x 0..3, then a cliff up to 0.4 for x 4..7
    static float[,] CliffMap() {
        float[,] map = new float[8, 3];
        for(int x = 0; x < 8; x++) {
            for(int y = 0; y < 3; y++) {
                map[x, y] = x < 4 ? 0.158f : 0.4f;
            }
        }
        return map;
    }

    // World-space normal of mesh vertex (x, y): undo the scale pre-multiplication
    static Vector3 WorldNormal(Vector3[] normals, MeshData meshData, int x, int y) {
        Vector3 n = normals[y * meshData.meshWidth + x];
        return new Vector3(n.x / scale.x, n.y / scale.y, n.z / scale.z).normalized;
    }

    [Test]
    public void FlatGroundAtCliffBaseStaysFlat() {
        float[,] map = CliffMap();
        MeshData meshData = new MeshData(10, 5, 10);   // +2 overhang each axis

        Vector3[] normals = MeshGenerator.CalculateNormals(meshData, map, 0, 0, scale);

        // Sample x=3 is the last flat sample; its mesh vertex is x+1
        Vector3 edge = WorldNormal(normals, meshData, 4, 2);
        Assert.Less(Vector3.Angle(edge, Vector3.up), 0.5f, $"flat edge normal tilted: {edge}");
    }

    [Test]
    public void GentleSlopesStillAverage() {
        float[,] map = new float[5, 1];
        for(int x = 0; x < 5; x++) {
            map[x, 0] = x * 0.01f;   // constant 0.1 world slope (~6 degrees)
        }
        MeshData meshData = new MeshData(7, 3, 10);

        Vector3[] normals = MeshGenerator.CalculateNormals(meshData, map, 0, 0, scale);

        Vector3 mid = WorldNormal(normals, meshData, 3, 1);
        Assert.AreEqual(Mathf.Atan(0.1f) * Mathf.Rad2Deg, Vector3.Angle(mid, Vector3.up), 0.1f);
        Assert.Less(mid.x, 0, "normal should lean away from the uphill (+x) side");
    }

    [Test]
    public void OffsetIntoGlobalMapMatchesNeighbourContainer() {
        // Two "containers" 4 samples wide over the same global map: container B's left overhang vertex (sample 3)
        // must get the same normal as container A's last real vertex (sample 3), so the seam lights continuously.
        float[,] map = CliffMap();
        MeshData a = new MeshData(6, 5, 10);
        MeshData b = new MeshData(6, 5, 10);

        Vector3[] na = MeshGenerator.CalculateNormals(a, map, 0, 0, scale);
        Vector3[] nb = MeshGenerator.CalculateNormals(b, map, 4, 0, scale);

        Assert.AreEqual(na[2 * a.meshWidth + 4], nb[2 * b.meshWidth + 0]);
        Assert.AreEqual(na[2 * a.meshWidth + 5], nb[2 * b.meshWidth + 1]);
    }
}
