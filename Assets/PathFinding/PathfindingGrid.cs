using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor; // handles

public class PathfindingGrid : MonoBehaviour, TerrainUpdateDelegate {

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

    private void OnDestroy() {
        try {
            Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
        } catch(System.NullReferenceException e) { }
    }

    public int maxSize {
        get {
            return gridSizeX * gridSizeY;
        }
    }

    public void SetCenterGridWalkable(LayoutCoordinate layoutCoordinate, bool walkable) {
        PathGridCoordinate[][] updatedPathGridCoordinates = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);

        PathGridCoordinate updateCoordinate = updatedPathGridCoordinates[1][1];

        grid[updateCoordinate.xLowSample, updateCoordinate.yLowSample].walkable = walkable;

        // Notify all users of path finding grid about ubdate
        Script.Get<MapsManager>().NotifyTerrainUpdateDelegates(layoutCoordinate);
    }

    public void UpdateGrid(Map map, LayoutCoordinate layoutCoordinate) {
        PathGridCoordinate[][] updatedPathGridCoordinates = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);
        BuildingManager buildingManager = Script.Get<BuildingManager>();

        int width = updatedPathGridCoordinates.GetLength(0);
        for(int x = 0; x < width; x++) {

            PathGridCoordinate[] updatedCoordinateColumn = updatedPathGridCoordinates[x];
            int height = updatedCoordinateColumn.GetLength(0);

            for(int y = 0; y < height; y++) {
                PathGridCoordinate updatedCoordinate = updatedCoordinateColumn[y];
                bool buildingBlocksSpace = false;

                // If we are in the center square, a building blocks our location if one exists at this layout coordinate
                if (x == width / 2 && y == height / 2) {
                    buildingBlocksSpace = buildingManager.buildlingAtLocation(layoutCoordinate) != null;
                }

                grid[updatedCoordinate.xLowSample, updatedCoordinate.yLowSample].walkable = map.GetTerrainAt(layoutCoordinate).walkable && !buildingBlocksSpace;
            }
        }

        // Notify all users of path finding grid about ubdate
        Script.Get<MapsManager>().NotifyTerrainUpdateDelegates(layoutCoordinate);
    }

    const int maxPenalty = 100;
    public void createGrid() {
        grid = new Node[gridSizeX, gridSizeY];
        MapsManager mapsManager = Script.Get<MapsManager>();

        for(int x = 0; x < gridSizeX; x++) {
            for(int y = 0; y < gridSizeY; y++) {

                PathGridCoordinate pathGridCoordinate = new PathGridCoordinate(x, y);

                MapCoordinate mapCoordinate = MapCoordinate.FromGridCoordinate(pathGridCoordinate);
                WorldPosition worldPosition = new WorldPosition(mapCoordinate);

                LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);                
                TerrainType terrain = mapsManager.GetTerrainAt(layoutCoordinate);

                int penalty = Mathf.FloorToInt(maxPenalty + (maxPenalty / 2.0f));

                if(terrain.walkable) {
                    penalty = Mathf.FloorToInt((1 - terrain.walkSpeedMultiplier) * maxPenalty);
                }              

                grid[x, y] = new Node(terrain.walkable, worldPosition, x, y, penalty);
            }
        }

        mapsManager.AddTerrainUpdateDelegate(this);
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
        PathGridCoordinate pathGridCoordinate = PathGridCoordinate.fromMapCoordinate(mapCoordinate);

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
        Constants constants = Script.Get<Constants>();

        if (constants == null) { return;  }

        float cubeDiameter = constants.layoutMapWidth / 3.5f;
        if(grid != null && displayGridGizmos && Script.Get<MapsManager>().mapContainers.Count > 0) {

            Vector3 mapScale = Script.Get<MapsManager>().mapContainers[0].transform.lossyScale;

            Transform mapsManagerPosition = Script.Get<MapsManager>().transform;
            Gizmos.DrawWireCube(mapsManagerPosition.position, new Vector3(gridSizeX * mapScale.x, 1 * mapScale.y, gridSizeY * mapScale.z));

            foreach(Node n in grid) {
                //Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));

                Gizmos.color = Color.Lerp(Color.green, Color.blue, Mathf.InverseLerp(0, 10, n.movementPenalty));

                //print(n.movementPenalty);


                Gizmos.color = n.walkable ? Gizmos.color : Color.red;

                Color col = Gizmos.color;
                col.a = 0.5f;

                Gizmos.color = col;

                //if (PathFinding.staticGridCoordinatesSurroundingLayoutCoordinate != null) {
                //    PathGridCoordinate pathGridCoordinate = new PathGridCoordinate(n.gridX, n.gridY);
                //    if(PathFinding.staticGridCoordinatesSurroundingLayoutCoordinate.Contains(pathGridCoordinate)) {
                //        Gizmos.color = Color.cyan;

                //        WorldPosition worldPosition = new WorldPosition(MapCoordinate.FromGridCoordinate(pathGridCoordinate));

                //        WorldPosition to = worldPosition;
                //        to.y = 100;

                //        Gizmos.DrawLine(worldPosition.vector3, to.vector3);
                //    }
                //}                 

                Gizmos.DrawCube(n.worldPosition.vector3, Vector3.one *cubeDiameter);
                //Handles.Label(n.worldPosition.vector3, n.gridX + ", " + n.gridY);
                //Handles.Label(n.worldPosition.vector3, "G: " + n.gCost + " H: " + n.hCost + "\n   F: " + n.fCost);
            }
        }
    }

    /*
     * TerrainUpdateDelegate Interface
     * */

    public void NotifyTerrainUpdate(LayoutCoordinate layoutCoordinate) {
        MapsManager mapsManager = Script.Get<MapsManager>();
        TerrainType terrain = mapsManager.GetTerrainAt(layoutCoordinate);

        int penalty = Mathf.FloorToInt(maxPenalty + (maxPenalty / 2.0f));

        if(terrain.walkable) {
            penalty = Mathf.FloorToInt((1 - terrain.walkSpeedMultiplier) * maxPenalty);
        }

        MapCoordinate mapCoordinate = new MapCoordinate(layoutCoordinate);
        PathGridCoordinate[][] pathGridCoordinates = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate);

        foreach(PathGridCoordinate[] coordinates in pathGridCoordinates) {
            foreach(PathGridCoordinate coordinate in coordinates) {
                grid[coordinate.xLowSample, coordinate.yLowSample].movementPenalty = penalty;
            }
        }
    }

    //[System.Serializable]
    //public class TerrainType {
    //    public LayerMask terrainMask;
    //    public int terrainPenalty;
    //}
}
