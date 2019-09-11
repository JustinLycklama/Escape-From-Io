using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathBuilding : Building {

    protected override string title => "Path";
    protected override float constructionModifierSpeed => 5;    

    protected override void CompleteBuilding() {
        WorldPosition worldPosition = new WorldPosition(transform.position);
        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);

        layoutCoordinate.mapContainer.map.UpdateTerrainAtLocation(layoutCoordinate, Script.Get<TerrainManager>().terrainTypeMap[TerrainType.Type.Path]);

        transform.SetParent(null);
        Destroy(gameObject);
    }

    protected override void UpdateCompletionPercent(float percent) {

    }
}
