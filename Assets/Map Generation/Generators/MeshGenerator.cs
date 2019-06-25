using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {   

    public static MeshData GenerateTerrainMesh(float[,] heightMap, int featuresPerLayoutPerAxis) {
        int width = heightMap.GetLength(0) + 2;
        int height = heightMap.GetLength(1) + 2;

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height, featuresPerLayoutPerAxis);

        for (int y = 0; y < height; y++) {

            // If we are on a y-row that needs duplicates, do the set of non-triangle verticies first
            if((y + 1) % featuresPerLayoutPerAxis == 0) {
                DoAllXFor(y, topLeftX, topLeftZ, width, height, heightMap, meshData, featuresPerLayoutPerAxis, false);
            }

            DoAllXFor(y, topLeftX, topLeftZ, width, height, heightMap, meshData, featuresPerLayoutPerAxis, true);
        }

        return meshData;
    }

    private static void DoAllXFor(int y, float topLeftX, float topLeftZ, int width, int height, float[,] heightMap, MeshData meshData, int featuresPerLayoutPerAxis, bool hasTriangles) {
        for(int x = 0; x < width; x++) {

            int boundedX = x - 1;
            int boundedY = y - 1;

            int clampedX = Mathf.Clamp(x - 1, 0, width - 3);
            int clampedY = Mathf.Clamp(y - 1, 0, height - 3);

            float heightAtIndex = -1f;
            if(!(boundedX < 0 || boundedX > width - 3 || boundedY < 0 || boundedY > height - 3)) {
                heightAtIndex = heightMap[clampedX, clampedY];
            }

            Vector3 vertex = new Vector3(topLeftX + x, heightAtIndex, topLeftZ - y);
            Vector2 uv = new Vector2(x / (float)width, y / (float)height);
            bool hasTriangleAssociatedToVertex = false;

            // hasTriangles means that this vertex should even consider triangles (we do not consider triangles for the duplicate x or y verticies)
            if(hasTriangles) {
                // There are no triangles associated with the right and bottom edge of the map as a whole, since there is no corresponding verticies next 
                hasTriangleAssociatedToVertex = x < width - 1 && y < height - 1;
            }

            // We are on tile edge
            if((x + 1) % featuresPerLayoutPerAxis == 0) {
                // On the tile edge, create two sets of verticies in the same spot

                // Add a closing set of verticies to finish off the tile. 
                meshData.addVertex(vertex, uv, false);
            }

            meshData.addVertex(vertex, uv, hasTriangleAssociatedToVertex);
        }
    }

    public static void UpdateTerrainMesh(MeshData meshData, float[,] heightMap, int featuresPerLayoutPerAxis, LayoutCoordinate layoutCoordinate) {
        // TODO: Optimize Terraform
        for(int y = 0; y < featuresPerLayoutPerAxis; y++) {
            int sampleY = layoutCoordinate.y * featuresPerLayoutPerAxis + y;
            int meshPositionY = layoutCoordinate.y * (featuresPerLayoutPerAxis + 1) + y + 1;

            for(int x = 0; x < featuresPerLayoutPerAxis; x++) {
                int sampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + x;
                int meshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + x + 1;

                float sampledHeight = heightMap[sampleX, sampleY];

                meshData.EditHeight(sampledHeight, meshPositionX, meshPositionY);
            }

            // We need to do the extra vertex at the end of the row, remember it has the same height as the one previous
            int lastSampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + featuresPerLayoutPerAxis - 1;
            int lastMeshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + featuresPerLayoutPerAxis + 1;

            float lastSampledHeight = heightMap[lastSampleX, sampleY];

            meshData.EditHeight(lastSampledHeight, lastMeshPositionX, meshPositionY);
        }

        // Now do the last row of the Y
        int lastSampleY = layoutCoordinate.y * featuresPerLayoutPerAxis + featuresPerLayoutPerAxis - 1;
        int lastMeshPositionY = layoutCoordinate.y * (featuresPerLayoutPerAxis + 1) + featuresPerLayoutPerAxis + 1;

        for(int x = 0; x < featuresPerLayoutPerAxis; x++) {
            int sampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + x;
            int meshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + x;

            float sampledHeight = heightMap[sampleX, lastSampleY];

            meshData.EditHeight(sampledHeight, meshPositionX, lastMeshPositionY);
        }

        // We need to do the extra vertex at the end of the row, remember it has the same height as the one previous
        int finalSampleX = layoutCoordinate.x * featuresPerLayoutPerAxis + featuresPerLayoutPerAxis - 1;
        int finalMeshPositionX = layoutCoordinate.x * (featuresPerLayoutPerAxis + 1) + featuresPerLayoutPerAxis + 1;

        float finalSampledHeight = heightMap[finalSampleX, lastSampleY];

        meshData.EditHeight(finalSampledHeight, finalMeshPositionX, lastMeshPositionY);
    }

    public static void UpdateMeshOverhang(MeshData meshData, MapContainerNeighbours neibours) {
        int width = meshData.meshWidth;
        int height = meshData.meshHeight;

        // Left edge
        int x = 0;
        int y = 0;

        for(y = 0; y < height; y++) {
            float finalSampleHeight = 0;

            if(y == 0 && neibours.topLeftMap != null) {
                MapCoordinate coordinate = new MapCoordinate(neibours.topLeftMap.map.mapWidth - 1, neibours.topLeftMap.map.mapHeight - 1, neibours.topLeftMap);
                finalSampleHeight = neibours.topLeftMap.map.getHeightAt(coordinate);
            }

            if(y > 0 && y < height - 1 && neibours.leftMap != null) {
                MapCoordinate coordinate = new MapCoordinate(neibours.leftMap.map.mapWidth - 1, (y - 1), neibours.leftMap);
                finalSampleHeight = neibours.leftMap.map.getHeightAt(coordinate);
            }

            if (y == height && neibours.bottomLeftMap != null ) {
                MapCoordinate coordinate = new MapCoordinate(neibours.bottomLeftMap.map.mapWidth - 1, 0, neibours.bottomLeftMap);
                finalSampleHeight = neibours.bottomLeftMap.map.getHeightAt(coordinate);
            }

            meshData.EditHeight(finalSampleHeight, x, y);
        }

        // Right edge
       /* x = width - 1;

        for(y = 0; y < height; y++) {
            float finalSampleHeight = 0;

            if(y == 0 && neibours.topRightMap != null) {
                MapCoordinate coordinate = new MapCoordinate(0, neibours.topRightMap.map.mapHeight - 1, neibours.topRightMap);
                finalSampleHeight = neibours.topRightMap.map.getHeightAt(coordinate);
            }

            if(y > 0 && y < height - 1 && neibours.rightMap != null) {
                MapCoordinate coordinate = new MapCoordinate(0, y - 1, neibours.rightMap);
                finalSampleHeight = neibours.rightMap.map.getHeightAt(coordinate);
            }

            if(y == height && neibours.bottomRightMap != null) {
                MapCoordinate coordinate = new MapCoordinate(0, 0, neibours.bottomRightMap);
                finalSampleHeight = neibours.bottomRightMap.map.getHeightAt(coordinate);
            }

            meshData.EditHeight(finalSampleHeight, x, y);
        }
        */
        // Top 

        y = 0;

        for(x = 1; x < width - 1; x++) {
            float finalSampleHeight = 0;

            if(neibours.topMap != null) {
                MapCoordinate coordinate = new MapCoordinate(x - 1, neibours.topMap.map.mapHeight - 1, neibours.topMap);
                finalSampleHeight = neibours.topMap.map.getHeightAt(coordinate);
            }

            meshData.EditHeight(finalSampleHeight, x, y);
        }

        // Bottom
        /*
        y = height - 1;

        for(x = 1; x < width - 1; x++) {
            float finalSampleHeight = 0;

            if(neibours.bottomMap != null) {
                MapCoordinate coordinate = new MapCoordinate(x - 1, 0, neibours.bottomMap);
                finalSampleHeight = neibours.bottomMap.map.getHeightAt(coordinate);
            }

            meshData.EditHeight(finalSampleHeight, x, y);
        }*/
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