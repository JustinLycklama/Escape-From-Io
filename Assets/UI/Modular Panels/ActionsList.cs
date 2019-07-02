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
    }

    public void SetActions(UserAction[] actions) {

        for(int i = 0; i < actionCellList.Length; i++) {
            ActionItemCell cell = actionCellList[i];

            UserAction action = null;
            if(actions != null && i < actions.Length) {
                action = actions[i];
            }

            cell.SetAction(action);
        }
    }
}
