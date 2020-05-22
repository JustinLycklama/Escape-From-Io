using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CostPanel : MonoBehaviour {    
    public ImageValueCell firstCell;
    public ImageValueCell secondCell;
    public ImageValueCell thirdCellll;

    public Color completeColor = Color.white;
    public Color notCompleteColor = Color.red;

    private List<ImageValueCell> orderedCells;

    private bool tallyMode = false;
    private BlueprintCost cost;
    protected Dictionary<MineralType, int> tallyCountDictionary = new Dictionary<MineralType, int>();

    //private int

    private void Awake() {
        orderedCells = new List<ImageValueCell>() { firstCell, secondCell, thirdCellll };        
    }

    public void SetCost(BlueprintCost cost) {

        tallyCountDictionary.Clear();
        foreach(MineralType mineralType in cost.costMap.Keys) {
            if(!tallyCountDictionary.ContainsKey(mineralType)) {
                tallyCountDictionary[mineralType] = 0;
            }
        }

        this.cost = cost;
        Display();
    }

    public void SetTallyMode(bool tallyMode) {
        this.tallyMode = tallyMode;
        Display();
    }

    public void TallyMineralType(MineralType mineralType) {
        tallyCountDictionary[mineralType]++;
        Display();
    }

    private void Display() {
        List<MineralType> sortedMineralTypes = cost.costMap.Keys.OrderBy(m => m.Order()).ToList();

        for(int i = 0; i < orderedCells.Count; i++) {
            ImageValueCell cell = orderedCells[i];

            if(i >= sortedMineralTypes.Count) {
                if(cell.gameObject.activeSelf) { cell.gameObject.SetActive(false); }
                continue;
            }

            MineralType mineralType = sortedMineralTypes[i];
            int costValue = cost.costMap[mineralType];

            if(!cell.gameObject.activeSelf) { cell.gameObject.SetActive(true); }

            cell.image.overrideSprite = mineralType.Icon();

            string total = costValue.ToString();

            if(tallyMode) {
                string tally = tallyCountDictionary[mineralType].ToString();
                cell.value.text = tally + "/" + total;

                if (tally != total) {
                    cell.backgroundImage.color = notCompleteColor;
                } else {
                    cell.backgroundImage.color = completeColor;
                }

            } else {
                cell.value.text = total;
            }
        }
    }
}

public static class MineralTypeSortExtensions {
    public static int Order(this MineralType mineralType) {
        switch(mineralType) {
            case MineralType.Copper:
                return 1;
            case MineralType.Silver:
                return 2;
            case MineralType.Gold:
                return 3;
            case MineralType.RefinedCopper:
                return 5;
            case MineralType.RefinedSilver:
                return 6;
            case MineralType.RefinedGold:
                return 7;
            case MineralType.Azure:
                return 4;
        }

        return 0;
    }

    public static Sprite Icon(this MineralType mineralType) {
        switch(mineralType) {
            case MineralType.Copper:
                return Script.Get<GameResourceManager>().rawCopperImage;
            case MineralType.Silver:
                return Script.Get<GameResourceManager>().rawSilverImage;
            case MineralType.Gold:
                return Script.Get<GameResourceManager>().rawGoldImage;
            case MineralType.RefinedCopper:
                return Script.Get<GameResourceManager>().refinedCopperImage;
            case MineralType.RefinedSilver:
                return Script.Get<GameResourceManager>().refinedSilverImage;
            case MineralType.RefinedGold:
                return Script.Get<GameResourceManager>().refinedGoldImage;
            case MineralType.Azure:
                return Script.Get<GameResourceManager>().azureImage;
        }

        return null;
    }
}
