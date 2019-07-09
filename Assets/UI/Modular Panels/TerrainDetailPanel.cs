using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDetailPanel : MonoBehaviour {

    public MasterAndGameTaskCell masterAndGameTaskCell;

    public TypeValueCell[] mineralTypeValues;

    MineralType[] mineralTypes;

    private void Start() {

        mineralTypes = new MineralType[] { MineralType.Copper, MineralType.Silver, MineralType.Gold };
       
        for(int i = 0; i < mineralTypes.Length; i++) {
            MineralType mineralType = mineralTypes[i];

            mineralTypeValues[i].type.text = mineralType.ToString();
        }
    }

    public void SetTerrain(LayoutCoordinate layoutCoordinate) {
        GameResourceManager gameResourceManager = Script.Get<GameResourceManager>();

        Dictionary<MineralType, int> mineralTypeCount = gameResourceManager.MineralListForCoordinate(layoutCoordinate);

        for(int i = 0; i < mineralTypes.Length; i++) {
            MineralType mineralType = mineralTypes[i];
            TypeValueCell cell = mineralTypeValues[i];

            if(mineralTypeCount.ContainsKey(mineralType)) {
                cell.value.text = mineralTypeCount[mineralType].ToString();
            } else {
                cell.value.text = "None";
            }
        }
    }
}
