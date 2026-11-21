using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {   

    // tileAligned (#35, the terrain): each tile's samples span the tile edge to edge (MapGenerator.SamplePosition), so a
    // tile's geometry ends exactly on its boundary. Otherwise (fog meshes) samples are spaced uniformly.
    public static MeshData GenerateTerrainMesh(float[,] heightMap, bool tileAligned = false) {
        Constants constants = Script.Get<Constants>();
        int f = constants.featuresPerLayoutPerAxis;

        int samplesX = heightMap.GetLength(0);
        int samplesY = heightMap.GetLength(1);
        int width = samplesX + 2;
        int height = samplesY + 2;

        MeshData meshData = new MeshData(width, height, f);

        for (int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                bool inside = x > 0 && x < width - 1 && y > 0 && y < height - 1;
                float heightAtIndex = inside ? heightMap[x - 1, y - 1] : 0f;

                float positionX;
                float positionY;
                if(tileAligned) {
                    // The overhang ring sits on the outer boundary, at the same position as the edge samples
                    positionX = Mathf.Clamp(MapGenerator.SamplePosition(x - 1, f), 0, samplesX);
                    positionY = Mathf.Clamp(MapGenerator.SamplePosition(y - 1, f), 0, samplesY);
                } else {
                    positionX = x - 0.5f;
                    positionY = y - 0.5f;
                }

                Vector3 vertex = new Vector3(positionX - samplesX / 2f, heightAtIndex, samplesY / 2f - positionY);
                Vector2 uv = new Vector2(x / (float)width, y / (float)height);

                // No triangles start on the last column or row
                meshData.addVertex(vertex, uv, x < width - 1 && y < height - 1);
            }
        }

        return meshData;
    }

    public static void UpdateTerrainMesh(MeshData meshData, float[,] heightMap, int featuresPerLayoutPerAxis, LayoutCoordinate layoutCoordinate) {
        // TODO: Optimize Terraform
        for(int y = 0; y < featuresPerLayoutPerAxis; y++) {
            int sampleY = layoutCoordinate.y * featuresPerLayoutPerAxis + y;
            int meshPositionY = sampleY + 1;

            for(int x = 0; x < featuresPerLayoutPerAxis; x++) {
                int sampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + x;
                int meshPositionX = sampleX + 1;

                float sampledHeight = heightMap[sampleX, sampleY];

                meshData.EditHeight(sampledHeight, meshPositionX, meshPositionY);
            }
        } 
    }

    // #25: vertex normals from the global height map, so they agree across map containers, computed in world proportions
    // (the container is scaled non-uniformly, e.g. 10/100/10) and creased at cliff feet and lips (the flatter side wins).
    // #35: samples are tile-aligned (MapGenerator.SamplePosition), so slopes use real sample spacing, and a tile's edge
    // sample is skipped over when it shares its position with the neighbouring tile's edge sample.
    // startSampleX/Y: global sample of this mesh's first non-overhang vertex. scale: the mesh's world scale.
    public static Vector3[] CalculateNormals(MeshData meshData, float[,] globalHeightMap, int startSampleX, int startSampleY, Vector3 scale, int samplesPerTile) {
        int width = meshData.meshWidth;
        int height = meshData.meshHeight;
        int globalWidth = globalHeightMap.GetLength(0);
        int globalHeight = globalHeightMap.GetLength(1);

        float slopeScaleX = scale.y / scale.x;
        float slopeScaleZ = scale.y / scale.z;

        Vector3[] normals = new Vector3[width * height];

        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                int globalX = startSampleX + x - 1;
                int globalY = startSampleY + y - 1;

                Vector3 worldNormal = Vector3.up;

                if(globalX >= 0 && globalX < globalWidth && globalY >= 0 && globalY < globalHeight) {
                    // World slope along +x, and along +y (which is -z in the mesh)
                    float slopeX = CreasedSlope(globalHeightMap, globalX, globalY, true, samplesPerTile, slopeScaleX);
                    float slopeY = CreasedSlope(globalHeightMap, globalX, globalY, false, samplesPerTile, slopeScaleZ);

                    worldNormal = new Vector3(-slopeX, 1, slopeY).normalized;
                }

                // Unity renders normals with the inverse transpose of the scale, so pre-multiply by the scale to get this world normal back
                normals[y * width + x] = Vector3.Scale(worldNormal, scale).normalized;
            }
        }

        return normals;
    }

    // World slope (rise over run) at a sample along x (alongX) or y. Averages the two sides, unless they differ by more than
    // creaseAngle (the foot or lip of a cliff), in which case the flatter side wins so flat ground stays flat-lit up to the edge.
    const float creaseAngle = 25f;

    private static float CreasedSlope(float[,] heightMap, int x, int y, bool alongX, int samplesPerTile, float slopeScale) {
        int here = alongX ? x : y;
        int limit = heightMap.GetLength(alongX ? 0 : 1);
        float position = MapGenerator.SamplePosition(here, samplesPerTile);

        float HeightAt(int index) => alongX ? heightMap[index, y] : heightMap[x, index];

        // Nearest sample on each side at a different position (a tile's edge sample shares its spot with the next tile's)
        int back = here - 1;
        if(back >= 0 && MapGenerator.SamplePosition(back, samplesPerTile) >= position - 0.001f) { back--; }
        int forward = here + 1;
        if(forward < limit && MapGenerator.SamplePosition(forward, samplesPerTile) <= position + 0.001f) { forward++; }

        bool hasBack = back >= 0;
        bool hasForward = forward < limit;

        float backSlope = hasBack ? (HeightAt(here) - HeightAt(back)) / (position - MapGenerator.SamplePosition(back, samplesPerTile)) * slopeScale : 0;
        float forwardSlope = hasForward ? (HeightAt(forward) - HeightAt(here)) / (MapGenerator.SamplePosition(forward, samplesPerTile) - position) * slopeScale : 0;

        if(!hasBack) { return forwardSlope; }
        if(!hasForward) { return backSlope; }

        float angleBetween = Mathf.Abs(Mathf.Atan(backSlope) - Mathf.Atan(forwardSlope)) * Mathf.Rad2Deg;
        if(angleBetween > creaseAngle) {
            return Mathf.Abs(backSlope) < Mathf.Abs(forwardSlope) ? backSlope : forwardSlope;
        }

        return (backSlope + forwardSlope) / 2f;
    }

    // The 1-vertex ring around a container mesh (#35). With tile-aligned samples the ring sits on the container boundary,
    // at the same position as the edge samples:
    // - left/top: the neighbouring container's edge samples, which normally equal ours, so the joining strip has zero size
    //   (it only shows as a wall if heights differ, e.g. mid-terraform)
    // - right/bottom: our own edge heights when a neighbour exists (zero-size strip; the neighbour draws the join), or 0 at
    //   the world border, which drops a skirt down the outside edge of the world
    public static void UpdateMeshOverhang(MeshData meshData, float[,] globalHeightMap, int startSampleX, int startSampleY) {
        int width = meshData.meshWidth;
        int height = meshData.meshHeight;
        int globalWidth = globalHeightMap.GetLength(0);
        int globalHeight = globalHeightMap.GetLength(1);
        int lastOwnX = startSampleX + width - 3;
        int lastOwnY = startSampleY + height - 3;

        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                if(x > 0 && x < width - 1 && y > 0 && y < height - 1) {
                    continue;
                }

                int globalX = startSampleX + x - 1;
                int globalY = startSampleY + y - 1;

                float overhangHeight = 0;
                if(globalX >= 0 && globalY >= 0 && globalX < globalWidth && globalY < globalHeight) {
                    overhangHeight = globalHeightMap[Mathf.Min(globalX, lastOwnX), Mathf.Min(globalY, lastOwnY)];
                }

                meshData.EditHeight(overhangHeight, x, y);
            }
        }
    }
}

public class MeshData {
    public Vector3[] verticies;
    public int[] triangles;

    // UVs are a relation for each of our verticies to the rest of mesh, in the x and y axis. 
    public Vector2[] uvs;

    public int meshWidth;
    public int meshHeight;

    int vertexIndex = 0;
    int triangleIndex = 0;

    Mesh mesh;

    public MeshData(int inputDataWidth, int inputDataHeight, int featuresPerLayoutPerAxis) {

        this.meshWidth = inputDataWidth; // + inputDataWidth / featuresPerLayoutPerAxis;
        this.meshHeight = inputDataHeight; // + inputDataHeight / featuresPerLayoutPerAxis;

        verticies = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(inputDataWidth - 1) * (inputDataHeight - 1) * 6];

        mesh = new Mesh();
    }

    public void addVertex(Vector3 vertex, Vector2 uv, bool hasTriangles) {

        verticies[vertexIndex] = vertex;
        uvs[vertexIndex] = uv;

        if (hasTriangles) {
            AddTriange(vertexIndex, vertexIndex + meshWidth + 1, vertexIndex + meshWidth);
            AddTriange(vertexIndex + meshWidth + 1, vertexIndex, vertexIndex + 1);
        }

        vertexIndex++;
    }

    public void EditHeight(float height, int x, int y) {
        int vertexIndex = (y * meshWidth) + x;

        verticies[vertexIndex].y = height;
    }

    private void AddTriange (int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;

        triangleIndex += 3;
    }

    // normals: optional precomputed normals (terrain uses MeshGenerator.CalculateNormals); otherwise Unity recalculates them
    public Mesh FinalizeMesh(Vector3[] normals = null) {
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if(normals != null) {
            mesh.normals = normals;
        } else {
            mesh.RecalculateNormals();
        }

        // The terrain shader converts its bump normal into tangent space, which needs real tangents (#27)
        mesh.RecalculateTangents();

        return mesh;
    }
}