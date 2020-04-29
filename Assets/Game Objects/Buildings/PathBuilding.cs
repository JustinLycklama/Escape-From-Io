using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathBuilding : Building {

    public override string title => "Path";
    public override float constructionModifierSpeed => 0.8f;    

    protected override void CompleteBuilding() {
        buildingLayoutCoordinate.mapContainer.map.UpdateTerrainAtLocation(buildingLayoutCoordinate, Script.Get<TerrainManager>().terrainTypeMap[TerrainType.Type.Path]);

        Destroy();
    }

    protected override void UpdateCompletionPercent(float percent) {

    }
}
