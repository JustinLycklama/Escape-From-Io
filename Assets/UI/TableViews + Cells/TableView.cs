using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface TableViewDelegate {
    int NumberOfRows(TableView table);
    void CellForRowAtIndex(TableView table, int row, GameObject cell);
}

public class TableView : MonoBehaviour
{
    public GameObject cellPrototype;
    public VerticalLayoutGroup layoutGroup;

    [HideInInspector]
    public TableViewDelegate dataDelegate;

    List<GameObject> cells;

    private void Awake() {
        cells = new List<GameObject>();
    }

    public void ReloadData() {
       
        // Size the table appropriately
        int requiredNumberOfCells = dataDelegate.NumberOfRows(this);

        if(cells.Count < requiredNumberOfCells) {
            for(int i = cells.Count; i < requiredNumberOfCells; i++) {
                GameObject newCell = Instantiate(cellPrototype);

                newCell.transform.SetParent(layoutGroup.transform, true);
                cells.Add(newCell);
            }
        } else if(cells.Count > requiredNumberOfCells) {
            for(int i = cells.Count - 1; i >= requiredNumberOfCells; i--) {
                Destroy(cells[i]);
                cells.RemoveAt(i);
            }
        }

        // Use Delegate to populate cells
        for (int i = 0; i < requiredNumberOfCells; i++) {
            dataDelegate.CellForRowAtIndex(this, i, cells[i]);
        }
    }    
}
