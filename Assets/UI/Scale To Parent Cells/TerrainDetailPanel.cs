using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDetailPanel : MonoBehaviour {

    public MasterAndGameTaskCell masterAndGameTaskCell;

    public TypeValueCell oreTypeValue;

    public void SetTerrain(LayoutCoordinate layoutCoordinate) {
        GameResourceManager gameResourceManager = Script.Get<GameResourceManager>();

        Dictionary<MineralType, int> mineralTypeCount = gameResourceManager.MineralListForCoordinate(layoutCoordinate);

        oreTypeValue.type.text = "Ore";

        if (mineralTypeCount.ContainsKey(MineralType.Ore)) {
            oreTypeValue.value.text = mineralTypeCount[MineralType.Ore].ToString();

        } else {
            oreTypeValue.value.text = "None";
        }

    }


}
