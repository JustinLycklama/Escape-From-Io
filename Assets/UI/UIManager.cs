using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class UIManager : MonoBehaviour, SelectionManagerDelegate, HotkeyDelegate
{
    public class Blueprint : PrefabBlueprint {
        private static string folder = "UI/";

        public Blueprint(string fileName,  Type type) : base(folder+fileName, type) { }

        //public static Blueprint PercentageBar = new Blueprint("PercentageBar", typeof(PercentageBar));
        public static Blueprint CostPanel = new Blueprint("CostPanelTooltip", typeof(CostPanelTooltip));
        public static Blueprint TaskAndUnitDetail = new Blueprint("TaskAndUnitDetailPanel", typeof(TaskAndUnitDetailPanel));
        public static Blueprint BlueprintPanel = new Blueprint("BlueprintPanel", typeof(BlueprintPanel));
        public static Blueprint CurrentSelectionPanel = new Blueprint("CurrentSelectionPanel", typeof(CurrentSelectionPanel));
    }

    //public Text selectionTitle;

    [SerializeField]
    public NavigationPanel currentTopPanel;

    [SerializeField]
    public GameButton tutorialRepeatButton;

    private SelectionManager selectionManager;
    private PlayerBehaviour playerBehaviour;


    private bool isRoot {
        get {
            return currentTopPanel.backTrace == null;
        }
    }

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
        if (currentTopPanel.backTrace == null) {
            return;
        }

        currentTopPanel = currentTopPanel.PopFromStack();

        // if we are at the root now, deselect everything
        if (currentTopPanel.backTrace == null) {
            selectionManager.RemoveSelection(false);
        }
    }

    private void Start() {
        selectionManager = Script.Get<SelectionManager>();
        selectionManager.RegisterForNotifications(this);

        playerBehaviour = Script.Get<PlayerBehaviour>();
        playerBehaviour.AddHotKeyDelegate(KeyCode.Q, this);
    }

    private void OnDestroy() {
        selectionManager.EndNotifications(this);
        playerBehaviour.RemoveHotKeyDelegate(this);
    }

    /*
     * SelectionManagerDelegate Implementation
     * */

    public void NotifyUpdateSelection(Selection selection) {
        if (isRoot && selection != null && selection.selectionType == Selection.SelectionType.Terrain) {
            var currentSelectionPanel = Push(Blueprint.CurrentSelectionPanel) as CurrentSelectionPanel;
            currentSelectionPanel.NotifyUpdateSelection(selection);
        }        
    }

    /*
     * HotkeyDelegate Interface
     * */

    public void HotKeyPressed(KeyCode key) {
        Pop();
    }

    //public void SetSelection(Selection selection) {
    //    this.selection = selection;

    //    selectionTitle.text = selection.Title();


    //    //actionsTable.ReloadData(this);

    //    //button.enabled = selection.selectionType == Selection.SelectionType.Terrain;

    //}
}
