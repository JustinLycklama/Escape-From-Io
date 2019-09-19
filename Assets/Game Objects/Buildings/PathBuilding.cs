using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathBuilding : Building {

    public override string title => "Path";
    protected override float constructionModifierSpeed => 0.8f;    

    protected override void CompleteBuilding() {
        WorldPosition worldPosition = new WorldPosition(transform.position);
        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

        layoutCoordinate.mapContainer.map.UpdateTerrainAtLocation(layoutCoordinate, Script.Get<TerrainManager>().terrainTypeMap[TerrainType.Type.Path]);

        Destroy();
    }

    protected override void UpdateCompletionPercent(float percent) {

    }
}
