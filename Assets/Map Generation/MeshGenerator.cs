using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height);

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                // There are no triangles associated with the right and bottom edge of the map, since there is no corresponding verticies next 
                bool hasTriangleAssociatedToVertex = x < width - 1 && y < height - 1;
                meshData.addVertex(new Vector3(topLeftX + x, heightMap[x, y], topLeftZ - y),                                        
                    new Vector2(x/(float)width, y/(float)height),
                    hasTriangleAssociatedToVertex);
            }
        }

        return meshData;
    }
}

public class MeshData {
    public Vector3[] verticies;
    public int[] triangles;

    // UVs are a relation for each of our verticies to the rest of mesh, in the x and y axis. 
    public Vector2[] uvs;

    int meshWidth;
    int meshHeight;

    int vertexIndex = 0;
    int triangleIndex = 0;

    public MeshData(int meshWidth, int meshHeight) {
        this.meshWidth = meshWidth;
        this.meshHeight = meshHeight;

        verticies = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
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

    private void AddTriange (int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;

        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}