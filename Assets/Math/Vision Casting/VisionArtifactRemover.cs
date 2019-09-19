using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionArtifactRemover {

    struct Coordinate {
        public int x, y;

        public Coordinate(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj) {
            if(!(obj is Coordinate)) {
                return false;
            }

            var coordinate = (Coordinate)obj;
            return x == coordinate.x &&
                   y == coordinate.y;
        }

        public override int GetHashCode() {
            var hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }
    }

    class IslandSet {
        public bool containsLight = false;
        public HashSet<Coordinate> coordinates;

        public IslandSet() {
            coordinates = new HashSet<Coordinate>();
        }
    }

    Constants constants;

    int width;
    int height;

    private BuildingEffectStatus[,] statusMap;
    private Building[,] buildingsEffectingStatusMapMap;

    private HashSet<Coordinate> testedCoordinates;

    public VisionArtifactRemover(BuildingEffectStatus[,] statusMap, Building[,] buildingsEffectingStatusMapMap) {
        this.statusMap = statusMap;
        this.buildingsEffectingStatusMapMap = buildingsEffectingStatusMapMap;

        constants = Script.Get<Constants>();

        width = constants.mapCountX * constants.layoutMapWidth;
        height = constants.mapCountY * constants.layoutMapHeight;

        testedCoordinates = new HashSet<Coordinate>();
    }

    // An isolated tile is one that is surrounded by 3+ dark spots
    public void RemoveLayerOfIsolatedTiles() {

        HashSet<Coordinate> isolatedCoordinates = new HashSet<Coordinate>();

        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                Coordinate coordinate = new Coordinate(x, y);

                if((statusMap[x, y] & BuildingEffectStatus.Light) == BuildingEffectStatus.Light) {

                    int adjacentDarkCount = 0;
                    List<Coordinate> adjacentTiles = GetAdjacentTiles(coordinate);
                    foreach(Coordinate adjacentCoordinate in adjacentTiles) {
                        if((statusMap[adjacentCoordinate.x, adjacentCoordinate.y] & BuildingEffectStatus.Light) == BuildingEffectStatus.None) {
                            adjacentDarkCount++;
                        }
                    }

                    if(adjacentDarkCount >= 3) {
                        isolatedCoordinates.Add(coordinate);
                    }
                }
            }
        }

        foreach(Coordinate coordinate in isolatedCoordinates) {
            statusMap[coordinate.x, coordinate.y] &= ~BuildingEffectStatus.Light;
        }
    }

    public void RemoveIslands() {     
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                Coordinate coordinate = new Coordinate(x, y);

                if (testedCoordinates.Contains(coordinate)) {
                    continue;
                }

                if (statusMap[x, y] == BuildingEffectStatus.None) {
                    continue;
                }

                if ((statusMap[x, y] & BuildingEffectStatus.Light) == BuildingEffectStatus.Light) {
                    IslandSet islandSet = new IslandSet();
                    BuildIsland(coordinate, islandSet);


                    // All coordinates in the island are to be considered tested
                    testedCoordinates.UnionWith(islandSet.coordinates);

                    // If the island set isn't lit, remove all lit values from set
                    if (!islandSet.containsLight) {
                        foreach(Coordinate islandCoordinate in islandSet.coordinates) {
                            statusMap[x, y] &= ~BuildingEffectStatus.Light;
                        }
                    }
                }

            }
        }
    }

    private void BuildIsland(Coordinate coordinate, IslandSet islandSet) {

        // If we have parsed this coordinate already, skip
        if (islandSet.coordinates.Contains(coordinate)) {
            return;
        }

        // If this coordinate is not lit, it is not a part of our island
        if ((statusMap[coordinate.x, coordinate.y] & BuildingEffectStatus.Light) == BuildingEffectStatus.None) {
            return;
        }

        islandSet.coordinates.Add(coordinate);        

        // If this coordinate has a lighting building, then this entire island is lit
        Building building = buildingsEffectingStatusMapMap[coordinate.x, coordinate.y];
        if (building != null && (building.BuildingStatusEffects() & BuildingEffectStatus.Light) == BuildingEffectStatus.Light) {
            islandSet.containsLight = true;
        }

        // Check 4 adjacent tiles and add them to our island if applicable
        List<Coordinate> extendIslandToCoordinates = GetAdjacentTiles(coordinate);

        foreach(Coordinate newCoordinate in extendIslandToCoordinates) {
            BuildIsland(newCoordinate, islandSet);
        }
    }

    private List<Coordinate> GetAdjacentTiles(Coordinate coordinate) {
        List<Coordinate> adjacentTiles = new List<Coordinate>();

        if(coordinate.x > 0) {
            adjacentTiles.Add(new Coordinate(coordinate.x - 1, coordinate.y));
        }

        if(coordinate.y > 0) {
            adjacentTiles.Add(new Coordinate(coordinate.x, coordinate.y - 1));
        }

        if(coordinate.x < width - 1) {
            adjacentTiles.Add(new Coordinate(coordinate.x + 1, coordinate.y));
        }

        if(coordinate.y < height - 1) {
            adjacentTiles.Add(new Coordinate(coordinate.x, coordinate.y + 1));
        }

        return adjacentTiles;
    }
}
 
