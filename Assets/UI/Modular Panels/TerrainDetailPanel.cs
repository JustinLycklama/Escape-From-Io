using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDetailPanel : MonoBehaviour {

    public MasterAndGameTaskCell masterAndGameTaskCell;

    public TypeValueCell[] mineralTypeValues;

    MineralType[] mineralTypes = new MineralType[] { MineralType.Copper, MineralType.Silver, MineralType.Gold };

    private void Start() {      
       
        for(int i = 0; i < mineralTypes.Length; i++) {
            MineralType mineralType = mineralTypes[i];

            mineralTypeValues[i].type.text = mineralType.ToString();
        }
    }

    public void SetTerrain(LayoutCoordinate layoutCoordinate) {
        GameResourceManager gameResourceManager = Script.Get<GameResourceManager>();

        Dictionary<MineralType, int> mineralTypeCount = gameResourceManager.MineralListForCoordinate(layoutCoordinate);

        TerrainType terrain = layoutCoordinate.mapContainer.map.GetTerrainAt(layoutCoordinate);

        for(int i = 0; i < mineralTypes.Length; i++) {

            MineralType mineralType = mineralTypes[i];
            TypeValueCell cell = mineralTypeValues[i];

            // Actual Mineral Info

            //if(mineralTypeCount.ContainsKey(mineralType)) {
            //    cell.value.text = mineralTypeCount[mineralType].ToString();
            //} else {
            //    cell.value.text = "None";
            //}

            // Mineral Chance Info

            TerrainType.MineralChance? mineralChance = null;

            foreach(TerrainType.MineralChance chance in terrain.mineralChances) {
                if (chance.type == mineralType) {
                    mineralChance = chance;
                    break;
                }
            }

            cell.value.text = mineralChance?.chance.NameAsRarity() ?? "None";
        }
    }
}
