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
            return constants.layoutMapWidth * constants.nodesPerLayoutPerAxis * constants.mapCountX;
        }
    }
        
    int gridSizeY {
        get {
            return constants.layoutMapHeight * constants.nodesPerLayoutPerAxis * constants.mapCountY;
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
            return gridSizeX * gridSizeY;
        }
    }

    public void UpdateGrid(Map map, LayoutCoordinate layoutCoordinate) {
        PathGridCoordinate[][] updatedPathGridCoordinates = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);

        foreach (PathGridCoordinate[] updatedCoordinateColumn in updatedPathGridCoordinates) {
            foreach(PathGridCoordinate updatedCoordinate in updatedCoordinateColumn) {
                grid[updatedCoordinate.xLowSample, updatedCoordinate.yLowSample].walkable = map.GetTerrainAt(layoutCoordinate).walkable;
            }
        }
    }

    public void createGrid() {
        //gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        //gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        grid = new Node[gridSizeX, gridSizeY];

        for(int x = 0; x < gridSizeX; x++) {
            for(int y = 0; y < gridSizeY; y++) {

                PathGridCoordinate pathGridCoordinate = new PathGridCoordinate(x + 0.5f, y + 0.5f);
                //MapCoordinate mapCoordinate = new MapCoordinate(pathGridCoordinate);

                WorldPosition worldPosition = WorldPosition.FromGridCoordinate(pathGridCoordinate);
                MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);                
                TerrainType terrain = Script.Get<MapsManager>().GetTerrainAt(layoutCoordinate);

                grid[x, y] = new Node(terrain.walkable, worldPosition, x, y, 0);
            }
        }

        // Second pass
        // Terrain where two of its four orthagonal neibours is unwalkable, is also unwalkable, to avoid walking over diagonals
        //bool[,] secondaryWalkable = new bool[gridSizeX, gridSizeY];
        //for(int x = 0; x < gridSizeX; x++) {
        //    for(int y = 0; y < gridSizeY; y++) {
        //        bool walkable = grid[x, y].walkable;

        //        if(walkable) {
        //            int otherUnwalkable = 0;

        //            for(int subX = -1; subX <= 1; subX += 2) {
        //                for(int subY = -1; subY <= 1; subY += 2) {
        //                    int sampleX = Mathf.Clamp(x + subX, 0, gridSizeX - 1);
        //                    int sampleY = Mathf.Clamp(y + subY, 0, gridSizeY - 1);

        //                    otherUnwalkable += grid[sampleX, sampleY].walkable ? 0 : 1;
        //                }
        //            }

        //            secondaryWalkable[x, y] = otherUnwalkable >= 2;
        //        }
        //    }
        //}

        //for(int x = 0; x < gridSizeX; x++) {
        //    for(int y = 0; y < gridSizeY; y++) {
        //        grid[x, y].walkable = grid[x, y].walkable && !secondaryWalkable[x, y];
        //    }
        //}

        //Vector3 mapScale = Tag.Map.GetGameObject().transform.localScale;
        //Vector3 worldBottomLeft = transform.position - (Vector3.right * gridSizeX * mapScale.x / 2) - (Vector3.forward * gridSizeY * mapScale.y / 2);

        //for (int x = 0; x < gridSizeX; x++) {
        //    for (int y = 0; y < gridSizeY; y++)
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

    public Node nodeFromWorldPoint(WorldPosition worldPos) {
        MapCoordinate mapCoordinate =  MapCoordinate.FromWorldPosition(worldPos);
        PathGridCoordinate pathGridCoordinate = new PathGridCoordinate(mapCoordinate);

        //float percentX = (worldPos.x + gridWorldSize.x / 2) / gridWorldSize.x;
        //float percentY = (worldPos.z + gridWorldSize.y / 2) / gridWorldSize.y;

        //percentX = Mathf.Clamp01(percentX);
        //percentY = Mathf.Clamp01(percentY);

        //int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        //int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[pathGridCoordinate.xLowSample, pathGridCoordinate.yLowSample];
    }

    public List<Node> GetNeighbours(Node node) {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) {
                    continue;
                }               

                int sampleX = node.gridX + x;
                int sampleY = node.gridY + y;

                // We are at the corner of our neibours
                if(x != 0 && y != 0) {

                    // Do not add this corner neibour if either of the neibours beside it are unwalkable from here
                    // In other words, only allow diagonal travel if we are not passing through the corner of an unwalkable tile
                    if ((sampleX >= 0 && sampleX < gridSizeX && sampleY >= 0 && sampleY < gridSizeY) &&
                        (!grid[sampleX, node.gridY].walkable || !grid[node.gridX, sampleY].walkable)) {
                        continue;
                    }
                }

                if (sampleX >= 0 && sampleX < gridSizeX && sampleY >= 0 && sampleY < gridSizeY) {                
                    neighbours.Add(grid[sampleX, sampleY]);
                }
            }
        }

        return neighbours;
    }

    void OnDrawGizmos() {
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

        float cubeDiameter = constants.layoutMapWidth / constants.nodesPerLayoutPerAxis;
        if(Script.Get<MapsManager>().mapContainers.Count > 0 && grid != null && displayGridGizmos) {

            Vector3 mapScale = Script.Get<MapsManager>().mapContainers[0].transform.localScale;

            Transform mapsManagerPosition = Script.Get<MapsManager>().transform;
            Gizmos.DrawWireCube(mapsManagerPosition.position, new Vector3(gridSizeX * mapScale.x, 1 * mapScale.y, gridSizeY * mapScale.z));

            foreach(Node n in grid) {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                Gizmos.color = n.walkable ? Gizmos.color : Color.red;

                Color col = Gizmos.color;
                col.a = 0.5f;

                Gizmos.color = col;

                 

                Gizmos.DrawCube(n.worldPosition.vector3, Vector3.one *cubeDiameter);

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
