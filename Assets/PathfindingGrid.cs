using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor; // handles

public class PathfindingGrid : MonoBehaviour {

    Constants constants;

    public bool displayGridGizmos;

    public LayerMask unwalkableMask;

    //public TerrainType[] walkableRegions;
    LayerMask walkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();

    Node[,] grid;

    int gridSizeX {
        get {
            return constants.layoutMapWidth * constants.featuresPerLayoutPerAxis;
        }
    }
        
    int gridSizeY {
        get {
            return constants.layoutMapHeight * constants.featuresPerLayoutPerAxis;
        }
    }

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    void Awake() {

        constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        //foreach(TerrainType region in walkableRegions) {
        //    walkableMask.value += region.terrainMask.value;            
        //    walkableRegionsDictionary.Add(Mathf.FloorToInt(Mathf.Log(region.terrainMask.value, 2)), region.terrainPenalty);
        //}       
    }

    public int maxSize {
        get {
            return constants.mapWidth * constants.mapHeight;
        }
    }

    public void createGrid(Map map = null) {
        //gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        //gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        grid = new Node[constants.mapWidth, constants.mapHeight];

        for(int x = 0; x < gridSizeX; x++) {
            for(int y = 0; y < gridSizeY; y++) {

                MapCoordinate mapCoordinate = new MapCoordinate(x, y);
                WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
                TerrainType terrain = map.GetTerrainAt(layoutCoordinate);

                grid[x, y] = new Node(terrain.walkable, worldPosition, x, y, 0);
            }
        }

        //Vector3 mapScale = Tag.Map.GetGameObject().transform.localScale;
        //Vector3 worldBottomLeft = transform.position - (Vector3.right * constants.mapWidth * mapScale.x / 2) - (Vector3.forward * constants.mapHeight * mapScale.y / 2);

        //for (int x = 0; x < constants.mapWidth; x++) {
        //    for (int y = 0; y < constants.mapHeight; y++)
        //    {
        //        Vector3 worldPoint = worldBottomLeft +
        //        Vector3.right * ((x * constants.nodesPerLayoutPerAxis) + constants.nodesPerLayoutPerAxis / 2f) +
        //        Vector3.forward * ((y * constants.nodesPerLayoutPerAxis) + constants.nodesPerLayoutPerAxis / 2f);

        //        bool walkable = false;
        //        int movementPenalty = 0;

        //        if (map == null) {
        //            walkable = !(Physics.CheckSphere(worldPoint, constants.nodesPerLayoutPerAxis / 2f, unwalkableMask));

        //            // Raycast to find layer to get penalty
        //            if(walkable) {
        //                Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
        //                RaycastHit hit;
        //                if(Physics.Raycast(ray, out hit, 100, walkableMask)) {
        //                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
        //                }

        //            } else {
        //                movementPenalty = 100;
        //            }
        //        } else {



        //        }



        //        grid[x, y] = new Node(walkable, new WorldPosition(worldPoint), x, y, movementPenalty);
        //    }
        //}
    }

    public void BlurPenaltyMap(int blurSize) {
        int kernelExtents = blurSize;

        int[,] horizontalPass = new int[constants.mapWidth, constants.mapHeight];
        int[,] verticalPass = new int[constants.mapWidth, constants.mapHeight];

        // Horizontal Pass
        for (int y = 0; y < constants.mapHeight; y++) {

            // First node we have sum everything
            for (int x = -kernelExtents; x<= kernelExtents; x++) {
                // clamp the x value for when the extents take the sample outside of our grid
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                horizontalPass[0, y] += grid[sampleX, y].movementPenalty;
            }

            // continue with the rest of the columns by remoing the far left item and adding the far right item to our previous value
            // There by imitating summing all of the values together
            for (int x = 1; x < constants.mapWidth; x++) {
                int removeIndex = Mathf.Clamp(x - kernelExtents, 0, constants.mapWidth - 1);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, constants.mapWidth - 1);

                horizontalPass[x, y] = horizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
            }
        }

        // Vertical Pass
        for(int x = 0; x < constants.mapWidth; x++) {

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
            for(int y = 1; y < constants.mapHeight; y++) {
                int removeIndex = Mathf.Clamp(y - kernelExtents, 0, constants.mapHeight - 1);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, constants.mapHeight - 1);

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

    public Node nodeFromWorldPoint(WorldPosition worldPos) {
        MapCoordinate mapCoordinate = new MapCoordinate(worldPos);

        //float percentX = (worldPos.x + gridWorldSize.x / 2) / gridWorldSize.x;
        //float percentY = (worldPos.z + gridWorldSize.y / 2) / gridWorldSize.y;

        //percentX = Mathf.Clamp01(percentX);
        //percentY = Mathf.Clamp01(percentY);

        //int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        //int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[mapCoordinate.xLowSample, mapCoordinate.yLowSample];
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
                
                if (checkX >= 0 && checkX < constants.mapWidth && checkY >= 0 && checkY < constants.mapHeight) {
                
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    void OnDrawGizmos() {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();
        Vector3 mapScale = Tag.Map.GetGameObject().transform.localScale;

        Gizmos.DrawWireCube(transform.position, new Vector3(constants.mapWidth * mapScale.x, 1 * mapScale.y, constants.mapHeight * mapScale.z));

        //float cubeDiameter = constants.mapWidth * mapScale.x / constants.featuresPerLayoutPerAxis / constants.nodesPerLayoutPerAxis;

        if(grid != null && displayGridGizmos) {
            foreach(Node n in grid) {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                Gizmos.color = n.walkable ? Gizmos.color : Color.red;

                Color col = Gizmos.color;
                col.a = 0.5f;

                Gizmos.color = col;

                 

                Gizmos.DrawCube(n.worldPosition.vector3, Vector3.one * mapScale.x);

                //Handles.Label(n.worldPosition.vector3, "G: " + n.gCost + " H: " + n.hCost + "\n   F: " + n.fCost);
            }
        }
    }

    //[System.Serializable]
    //public class TerrainType {
    //    public LayerMask terrainMask;
    //    public int terrainPenalty;
    //}
}
