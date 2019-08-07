﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentSelectionPanel : MonoBehaviour, SelectionManagerDelegate, TaskStatusUpdateDelegate, UserActionUpdateDelegate {

    Selection currentSelection;

    const string noSelectionText = "None";
    public Text title;

    //public MasterAndGameTaskCell taskItemCell;

    public UnitDetailPanel unitDetailPanel;
    public TerrainDetailPanel terrainDetailPanel;
    public ActionsList actionsList;

    public CameraPanel cameraPanel;

    void Start() {
        title.text = noSelectionText;
        Script.Get<SelectionManager>().RegisterForNotifications(this);

        unitDetailPanel.gameObject.SetActive(false);
        terrainDetailPanel.gameObject.SetActive(false);
    }

    private void OnDestroy() {
        Script.Get<SelectionManager>().EndNotifications(this);
    }

    private void SetActiveDetail(MonoBehaviour activePanel) {

        MonoBehaviour[] allPanels = new MonoBehaviour[] { unitDetailPanel, terrainDetailPanel };

        foreach(MonoBehaviour panel in allPanels) {
            if (panel != activePanel && panel.gameObject.activeSelf) {
                panel.gameObject.SetActive(false);
            }
        }

        if (activePanel != null && activePanel.gameObject.activeSelf == false) {
            activePanel.gameObject.SetActive(true);
        }
    }

    /*
     * SelectionManagerDelegate Interface
     * */

    public void NotifyUpdateSelection(Selection nextSelection) {

        //if(currentSelection != null && currentSelection.selectionType == Selection.SelectionType.Selectable && currentSelection.selection is TaskStatusNotifiable) {
        //    (currentSelection.selection as TaskStatusNotifiable).EndNotifications(this);
        //}

        cameraPanel.SetSelection(nextSelection);

        if (currentSelection != null) {
            currentSelection.EndSubscriptionToUserActions(this);
            currentSelection.EndSubscriptionToTaskStatus(this);

            if(currentGameAndTaskCell != null) {
                currentGameAndTaskCell.SetTask(null, null);
            }
        }

        if (nextSelection != null) {
            title.text = nextSelection.Title();
              
            if (nextSelection.selection is Unit) {
                SetActiveDetail(unitDetailPanel);

                unitDetailPanel.SetUnit(nextSelection.selection as Unit);
                currentGameAndTaskCell = unitDetailPanel.masterAndGameTaskCell;

            } else if(nextSelection.selectionType == Selection.SelectionType.Terrain) {
                SetActiveDetail(terrainDetailPanel);

                terrainDetailPanel.SetTerrain(nextSelection.coordinate);
                currentGameAndTaskCell = terrainDetailPanel.masterAndGameTaskCell;
            }

            nextSelection.SubscribeToUserActions(this);
            nextSelection.SubscribeToTaskStatus(this);

        } else {
            title.text = noSelectionText;
            actionsList.SetActions(new UserAction[] { });

            SetActiveDetail(null);
            currentGameAndTaskCell = null;
        }

        currentSelection = nextSelection;
    }

    /*
     * TaskStatusUpdateDelegate Interface
     * */
    private MasterAndGameTaskCell currentGameAndTaskCell;

    public void NowPerformingTask(Unit unit, MasterGameTask masterGameTask, GameTask gameTask) {
        if (currentGameAndTaskCell != null) {
            currentGameAndTaskCell.SetTask(masterGameTask, gameTask);
        }
    }

    /*
     * UserActionUpdateDelegate Interface
     * */

    public void UpdateUserActionsAvailable(UserAction[] userActions) {
        actionsList.SetActions(userActions);
    }
}
