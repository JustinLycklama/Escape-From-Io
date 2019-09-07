using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class UIManager : MonoBehaviour
{
    public class Blueprint : PrefabBlueprint {
        private static string folder = "UI/";

        public Blueprint(string fileName,  Type type) : base(folder+fileName, type) { }

        public static Blueprint PercentageBar = new Blueprint("PercentageBar", typeof(PercentageBar));
        public static Blueprint CostPanel = new Blueprint("CostPanelTooltip", typeof(CostPanelTooltip));
        public static Blueprint TaskAndUnitDetail = new Blueprint("TaskAndUnitDetailPanel", typeof(TaskAndUnitDetailPanel));
        public static Blueprint BlueprintPanel = new Blueprint("BlueprintPanel", typeof(BlueprintPanel));

        public static Blueprint MessageWindow = new Blueprint("MessageWindow", typeof(MessageWindow));
    }

    //public Text selectionTitle;

    public NavigationPanel currentTopPanel;

    public NavigationPanel Push(Blueprint blueprint) {
        NavigationPanel panel = blueprint.Instantiate() as NavigationPanel;
        panel.PushOntoStackFrom(currentTopPanel);
        currentTopPanel = panel;

        return panel;
    }

    public void PopToRoot() {
        while (currentTopPanel.backTrace != null) {
            Pop();
        }
    }

    public void Pop() {
        currentTopPanel = currentTopPanel.PopFromStack();
    }

    //public void SetSelection(Selection selection) {
    //    this.selection = selection;

    //    selectionTitle.text = selection.Title();
        

    //    //actionsTable.ReloadData(this);

    //    //button.enabled = selection.selectionType == Selection.SelectionType.Terrain;

    //}
}
