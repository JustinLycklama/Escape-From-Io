using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintPanel : NavigationPanel, TableViewDelegate {

    public Text tableTitle;
    public TableView blueprintTable;

    private LayoutCoordinate blueprintLayoutCoordinate;
    private ConstructionBlueprint[] blueprints = new ConstructionBlueprint[0];

    public void SetData(string title, ConstructionBlueprint[] blueprints, LayoutCoordinate blueprintLayoutCoordinate) {
        tableTitle.text = title;
        blueprintTable.dataDelegate = this;

        this.blueprintLayoutCoordinate = blueprintLayoutCoordinate;

        this.blueprints = blueprints;
        blueprintTable.ReloadData();
    }

    /*
     * TableViewDelegate Interface
     * */

    public int NumberOfRows(TableView table) {
        return blueprints.Length;
    }

    public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
        BlueprintCell blueprintCell = cell.GetComponent<BlueprintCell>();
        ConstructionBlueprint blueprint = blueprints[row];

        blueprintCell.SetBlueprint(blueprint, blueprintLayoutCoordinate);
        blueprintCell.icon.sprite = blueprint.iconImage;
    }
}
