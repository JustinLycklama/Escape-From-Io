using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighscoreList : TabElement, TableViewDelegate {
    public TableView table;
   
    private void Start() {
        table.dataDelegate = this;
        table.ReloadData();
    }

    /*
     * TableViewDelegate Interface
     * */

    public int NumberOfRows(TableView table) {
        return 2;
    }

    public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
        HighscoreCell hsCell = cell.GetComponent<HighscoreCell>();
        hsCell.SetIsSubmissionCell(false);
    }
}
