using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostPanel : MonoBehaviour
{
    Dictionary<MineralType, ImageValueCell> imageValueDictionary;

    public ImageValueCell copperCell;
    public ImageValueCell silverCell;
    public ImageValueCell goldCell;

    private bool tallyMode = false;
    private BlueprintCost cost;
    private Dictionary<MineralType, int> tallyCountDictionary = new Dictionary<MineralType, int>();

    //private int

    private void Awake() {
        copperCell.image.overrideSprite = Script.Get<GameResourceManager>().oreImage;

        imageValueDictionary = new Dictionary<MineralType, ImageValueCell>() { { MineralType.Ore, copperCell }, { MineralType.Silver, silverCell }, { MineralType.Gold, goldCell } };

        foreach(MineralType mineralType in imageValueDictionary.Keys) {
            if(!tallyCountDictionary.ContainsKey(mineralType)) {
                tallyCountDictionary[mineralType] = 0;
            }
        }

        //imageValueDictionary = new Dictionary<MineralType, ImageValueCell>() { { MineralType.Ore, copperCell }, { MineralType.Silver, silverCell }, { MineralType.Gold, goldCell } };
    }

    public void SetCost(BlueprintCost cost) {
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

        if (imageValueDictionary == null) {
            Init();
        }

        foreach(MineralType mineralType in imageValueDictionary.Keys) {
            ImageValueCell cell = imageValueDictionary[mineralType];

            string total = cost.costMap[mineralType].ToString();

            if(tallyMode) {
                string tally = tallyCountDictionary[mineralType].ToString();
                cell.value.text = tally + "/" + total;
            } else {
                cell.value.text = total;
            }
        }     
    }

    private void Init() {
      
    }
}
