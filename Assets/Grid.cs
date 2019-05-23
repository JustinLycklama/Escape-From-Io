using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor; // handles

public class Grid : MonoBehaviour {

    public bool displayGridGizmos;

    public LayerMask unwalkableMask;

    public Vector2 gridWorldSize;
    public float nodeRaduis;
    public TerrainType[] walkableRegions;
    LayerMask walkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();

    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    void Awake() {
        nodeDiameter = nodeRaduis * 2;

        foreach (TerrainType region in walkableRegions) {
            walkableMask.value += region.terrainMask.value;            
            walkableRegionsDictionary.Add(Mathf.FloorToInt(Mathf.Log(region.terrainMask.value, 2)), region.terrainPenalty);
        }       
    }

    public int maxSize {
        get {
            return gridSizeX * gridSizeY;
        }
    }

    public void createGrid(Map map = null) {
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        grid = new Node[gridSizeX, gridSizeY];

        Vector3 worldBottomLeft = transform.position - (Vector3.right * gridWorldSize.x / 2) - (Vector3.forward * gridWorldSize.y / 2);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft +
                Vector3.right * ((x * nodeDiameter) + nodeRaduis) +
                Vector3.forward * ((y * nodeDiameter) + nodeRaduis);

                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRaduis, unwalkableMask));

                int movementPenalty = 0;

                // Raycast to find layer to get penalty
                if (walkable) {
                    Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 100, walkableMask)) {
                        walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                    }
              
                } else {
                    movementPenalty = 100;
                }

                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }
    }   

    public void BlurPenaltyMap(int blurSize) {
        int kernelExtents = blurSize;

        int[,] horizontalPass = new int[gridSizeX, gridSizeY];
        int[,] verticalPass = new int[gridSizeX, gridSizeY];

        // Horizontal Pass
        for (int y = 0; y < gridSizeY; y++) {

            // First node we have sum everything
            for (int x = -kernelExtents; x<= kernelExtents; x++) {
                // clamp the x value for when the extents take the sample outside of our grid
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                horizontalPass[0, y] += grid[sampleX, y].movementPenalty;
            }

            // continue with the rest of the columns by remoing the far left item and adding the far right item to our previous value
            // There by imitating summing all of the values together
            for (int x = 1; x < gridSizeX; x++) {
                int removeIndex = Mathf.Clamp(x - kernelExtents, 0, gridSizeX - 1);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                horizontalPass[x, y] = horizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
            }
        }

        // Vertical Pass
        for(int x = 0; x < gridSizeX; x++) {

            //print("Penalties: \n");

            // First node we have sum everything
            for(int y = -kernelExtents; y <= kernelExtents; y++) {
                // clamp the x value for when the extents take the sample outside of our grid
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                verticalPass[x, 0] += horizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt(verticalPass[x, 0] / ((blurSize * 2 + 1) * (blurSize * 2 + 1)));
            grid[x, 0].movementPenalty = blurredPenalty;

            //string weights = "";

            // continue with the rest of the columns by remoing the far left item and adding the far right item to our previous value
            // There by imitating summing all of the values together
            for(int y = 1; y < gridSizeY; y++) {
                int removeIndex = Mathf.Clamp(y - kernelExtents, 0, gridSizeY - 1);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                verticalPass[x, y] = verticalPass[x, y-1] - horizontalPass[x, removeIndex] + horizontalPass[x, addIndex];

                blurredPenalty =  Mathf.RoundToInt(verticalPass[x, y] / ((blurSize * 2 + 1) * (blurSize * 2 + 1)));
                grid[x, y].movementPenalty = blurredPenalty;

                if (blurredPenalty > penaltyMax) {
                    penaltyMax = blurredPenalty;
                }

                if (blurredPenalty < penaltyMin) {
                    penaltyMin = blurredPenalty;
                }

                //weights += blurredPenalty + " ";
            }

            //print(weights);

        }
    }

    public Node nodeFromWorldPoint(Vector3 worldPos) {
        float percentX = (worldPos.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPos.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    public List<Node> GetNeighbours(Node node) {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) {
                    continue;
                }

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
                
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if(grid != null && displayGridGizmos) {
            foreach(Node n in grid) {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                Gizmos.color = n.walkable ? Gizmos.color : Color.red;

                Gizmos.DrawCube(n.worldPosition, Vector3.one * nodeDiameter);

                //Handles.Label(n.worldPosition, "G: " + n.gCost + " H: " + n.hCost + "\n   F: " + n.fCost);
            }
        }
    }

    [System.Serializable]
    public class TerrainType {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }
}
