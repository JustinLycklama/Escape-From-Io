using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, int featuresPerLayoutPerAxis) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height, featuresPerLayoutPerAxis);

        for (int y = 0; y < height; y++) {

            // If we are on a y-row that needs duplicates, do the set of non-triangle verticies first
            if((y + 1) % featuresPerLayoutPerAxis == 0) {
                for(int x = 0; x < width; x++) {
                    meshData.addVertex(new Vector3(topLeftX + x, heightMap[x, y], topLeftZ - y),
                        new Vector2(x / (float)width, y / (float)height),
                        false);

                    // We are on tile edge
                    if((x + 1) % featuresPerLayoutPerAxis == 0) {
                        // On the tile edge, create two sets of verticies in the same spot

                        // Add a closing set of verticies to finish off the tile. 
                        meshData.addVertex(new Vector3(topLeftX + x, heightMap[x, y], topLeftZ - y),
                            new Vector2(x / (float)width, y / (float)height),
                            false);
                    }
                }
            }

            for(int x = 0; x < width; x++) {
                // There are no triangles associated with the right and bottom edge of the map as a whole, since there is no corresponding verticies next 
                bool hasTriangleAssociatedToVertex = x < width - 1 && y < height - 1;

                // We are on tile edge
                if((x + 1) % featuresPerLayoutPerAxis == 0) {
                    // On the tile edge, create two sets of verticies in the same spot

                    // Add a closing set of verticies to finish off the tile. 
                    meshData.addVertex(new Vector3(topLeftX + x, heightMap[x, y], topLeftZ - y),
                        new Vector2(x / (float)width, y / (float)height),
                        false);
                }

                // Create an opening set of verticies with triangles to start the next tile, or continue our tile progress
                meshData.addVertex(new Vector3(topLeftX + x, heightMap[x, y], topLeftZ - y),                                        
                    new Vector2(x/(float)width, y/(float)height),
                    hasTriangleAssociatedToVertex);
            }
        }

        return meshData;
    }

    public static void UpdateTerrainMesh(MeshData meshData, float[,] heightMap, int featuresPerLayoutPerAxis, LayoutCoordinate layoutCoordinate) {
        // TODO: Optimize Terraform
        for(int y = 0; y < featuresPerLayoutPerAxis; y++) {
            int sampleY = layoutCoordinate.y * featuresPerLayoutPerAxis + y;
            int meshPositionY = layoutCoordinate.y * (featuresPerLayoutPerAxis + 1) + y;

            for(int x = 0; x < featuresPerLayoutPerAxis; x++) {
                int sampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + x;
                int meshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + x;

                float sampledHeight = heightMap[sampleX, sampleY];

                meshData.EditHeight(sampledHeight, meshPositionX, meshPositionY);
            }

            // We need to do the extra vertex at the end of the row, remember it has the same height as the one previous
            int lastSampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + featuresPerLayoutPerAxis - 1;
            int lastMeshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + featuresPerLayoutPerAxis;

            float lastSampledHeight = heightMap[lastSampleX, sampleY];

            meshData.EditHeight(lastSampledHeight, lastMeshPositionX, meshPositionY);
        }

        // Now do the last row of the Y
        int lastSampleY = layoutCoordinate.y * featuresPerLayoutPerAxis + featuresPerLayoutPerAxis - 1;
        int lastMeshPositionY = layoutCoordinate.y * (featuresPerLayoutPerAxis + 1) + featuresPerLayoutPerAxis;

        for(int x = 0; x < featuresPerLayoutPerAxis; x++) {
            int sampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + x;
            int meshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + x;

            float sampledHeight = heightMap[sampleX, lastSampleY];

            meshData.EditHeight(sampledHeight, meshPositionX, lastMeshPositionY);
        }

        // We need to do the extra vertex at the end of the row, remember it has the same height as the one previous
        int finalMeshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + featuresPerLayoutPerAxis;
        int finalSampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + featuresPerLayoutPerAxis - 1;

        float finalSampledHeight = heightMap[finalSampleX, lastSampleY];

        meshData.EditHeight(finalSampledHeight, finalMeshPositionX, lastMeshPositionY);
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

    Mesh mesh;

    public MeshData(int inputDataWidth, int inputDataHeight, int featuresPerLayoutPerAxis) {

        this.meshWidth = inputDataWidth + inputDataWidth / featuresPerLayoutPerAxis;
        this.meshHeight = inputDataHeight + inputDataHeight / featuresPerLayoutPerAxis;

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

    public Mesh FinalizeMesh() {
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}