using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionsList : MonoBehaviour {
    public ActionItemCell actionCell1;
    public ActionItemCell actionCell2;
    public ActionItemCell actionCell3;
    public ActionItemCell actionCell4;

    private ActionItemCell[] actionCellList;

    private void Awake() {
        actionCellList = new ActionItemCell[] { actionCell1, actionCell2, actionCell3, actionCell4 };
        SetActions(new UserAction[] { });
    }

    private void Start() {
        actionCell1.SetHotKey(KeyCode.E);
    }

    public void SetActions(UserAction[] actions) {

        for(int i = 0; i < actionCellList.Length; i++) {
            ActionItemCell cell = actionCellList[i];

            UserAction action = null;
            if(actions != null && i < actions.Length) {
                action = actions[i];
            }

            // Only show slots that have an action (the command card lays them out in a row).
            // Activate before SetAction: a slot hidden early may not have run its cell's Awake yet.
            GameObject slot = cell.transform.parent.gameObject;
            if (action != null) {
                if (!slot.activeSelf) {
                    slot.SetActive(true);
                }
                cell.SetAction(action);
            } else if (slot.activeSelf) {
                cell.SetAction(null);
                slot.SetActive(false);
            }
        }
    }
}
