using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainDetailPanel : MonoBehaviour {

    public MasterAndGameTaskCell masterAndGameTaskCell;

    public TypeValueCell[] mineralTypeValues;

    public CanvasGroup movementGroup;
    public CanvasGroup modificationGroup;

    public Text movementText;
    public Text movementValue;

    public Text modificationText;
    public Text modificationValue;

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

            if ((mineralChance?.chance ?? Chance.Impossible) == Chance.Guarenteed) {
                cell.value.text = mineralChance.Value.maxNumberGenerated.ToString();
            }

            if(cell.gameObject.activeSelf != (mineralChance != null)) {
                cell.gameObject.SetActive((mineralChance != null));
            }
        }

        // Set Modifier data
        bool movementActive = terrain.walkSpeedMultiplier != 0 && terrain.walkable;
        movementGroup.alpha = movementActive ? 1 : 0;
        
        bool modificationActive = terrain.modificationSpeedModifier != 0;
        modificationGroup.alpha = modificationActive ? 1 : 0;

        if(terrain.regionType == RegionType.Type.Mountain) {
            modificationText.text = "Mine Speed";
        } else {
            modificationText.text = "Build Speed";
        }

        movementValue.text = Mathf.RoundToInt(terrain.walkSpeedMultiplier * 100).ToString() + "%";
        modificationValue.text = Mathf.RoundToInt(terrain.modificationSpeedModifier * 100).ToString() + "%";
    }
}
