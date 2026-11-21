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

        Vector3[] normals = MeshGenerator.CalculateNormals(meshData, map, 0, 0, scale, 10);

        // Sample x=3 is the last flat sample; its mesh vertex is x+1
        Vector3 edge = WorldNormal(normals, meshData, 4, 2);
        Assert.Less(Vector3.Angle(edge, Vector3.up), 0.5f, $"flat edge normal tilted: {edge}");
    }

    [Test]
    public void GentleSlopesStillAverage() {
        float[,] map = new float[5, 1];
        for(int x = 0; x < 5; x++) {
            map[x, 0] = x * 0.01f;   // 1 height-unit per sample over 10/9 sample spacing (#35) = 0.09 world slope
        }
        MeshData meshData = new MeshData(7, 3, 10);

        Vector3[] normals = MeshGenerator.CalculateNormals(meshData, map, 0, 0, scale, 10);

        Vector3 mid = WorldNormal(normals, meshData, 3, 1);
        Assert.AreEqual(Mathf.Atan(0.09f) * Mathf.Rad2Deg, Vector3.Angle(mid, Vector3.up), 0.1f);
        Assert.Less(mid.x, 0, "normal should lean away from the uphill (+x) side");
    }

    [Test]
    public void OffsetIntoGlobalMapMatchesNeighbourContainer() {
        // Two "containers" 4 samples wide over the same global map: container B's left overhang vertex (sample 3)
        // must get the same normal as container A's last real vertex (sample 3), so the seam lights continuously.
        float[,] map = CliffMap();
        MeshData a = new MeshData(6, 5, 10);
        MeshData b = new MeshData(6, 5, 10);

        Vector3[] na = MeshGenerator.CalculateNormals(a, map, 0, 0, scale, 10);
        Vector3[] nb = MeshGenerator.CalculateNormals(b, map, 4, 0, scale, 10);

        Assert.AreEqual(na[2 * a.meshWidth + 4], nb[2 * b.meshWidth + 0]);
        Assert.AreEqual(na[2 * a.meshWidth + 5], nb[2 * b.meshWidth + 1]);
    }

    // #35: tile-aligned samples. A tile's first and last samples sit on its boundaries, so neighbouring tiles' edge samples
    // share a position and no geometry straddles a boundary.
    [Test]
    public void EdgeSamplesSitOnTileBoundaries() {
        Assert.AreEqual(0f, MapGenerator.SamplePosition(0, 10), 1e-5f);
        Assert.AreEqual(10f, MapGenerator.SamplePosition(9, 10), 1e-5f);
        Assert.AreEqual(10f, MapGenerator.SamplePosition(10, 10), 1e-5f);
        Assert.AreEqual(20f, MapGenerator.SamplePosition(19, 10), 1e-5f);
        Assert.AreEqual(0f, MapGenerator.SamplePosition(-1, 10), 1e-5f, "the overhang before sample 0 is the previous tile's last sample");
    }

    [Test]
    public void SlopeSkipsTheSharedBoundaryPosition() {
        // Flat at 0 for tile 0, then rising 0.01 per sample in tile 1: the shared boundary samples (9 and 10) both sit at
        // position 10, so the slope at sample 10 must come from sample 11, not from the zero-width step to sample 9.
        float[,] map = new float[20, 1];
        for(int x = 10; x < 20; x++) {
            map[x, 0] = (x - 10) * 0.01f;
        }
        MeshData meshData = new MeshData(22, 3, 10);

        Vector3[] normals = MeshGenerator.CalculateNormals(meshData, map, 0, 0, scale, 10);

        Vector3 inside = WorldNormal(normals, meshData, 15, 1);   // sample 14, mid-slope
        Assert.AreEqual(Mathf.Atan(0.09f) * Mathf.Rad2Deg, Vector3.Angle(inside, Vector3.up), 0.1f);
        Assert.IsFalse(float.IsNaN(WorldNormal(normals, meshData, 11, 1).x), "boundary sample normal must be finite");
    }
}
