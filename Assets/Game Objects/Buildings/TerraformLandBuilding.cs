using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerraformLandBuilding : Building {

    public override string title => "TerraformLand";
    protected override float constructionModifierSpeed => 0.05f;

    LayoutCoordinate layoutCoordinate;

    protected override void Awake() {
        base.Awake();

        WorldPosition worldPosition = new WorldPosition(transform.position);
        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

        layoutCoordinate = new LayoutCoordinate(mapCoordinate);
    }

    protected override void CompleteBuilding() {
        //layoutCoordinate.mapContainer.map.UpdateTerrainAtLocation(layoutCoordinate, Script.Get<TerrainManager>().terrainTypeMap[TerrainType.Type.Mud]);
        Destroy();
    }

    protected override void UpdateCompletionPercent(float percent) {
        layoutCoordinate.mapContainer.map.TerraformAtLocation(layoutCoordinate, percent, false);
    }
}
