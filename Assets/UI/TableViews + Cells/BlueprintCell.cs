﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlueprintCell : MonoBehaviour, IPointerClickHandler, BuildingsUpdateDelegate, TerrainUpdateDelegate {

    public Image icon;
    public UnitTypeIcon typeIcon;

    public Text labelText;
    public Text detailText;

    public CostPanel costPanel;

    public CanvasGroup canvasGroup;

    public GameObject disabledElement;
    public Text disabledText;

    private bool disabled = false;

    private ConstructionBlueprint blueprint;
    private LayoutCoordinate blueprintLayoutCoordinate;

    private void Start() {
        Script.Get<BuildingManager>().RegisterFoBuildingNotifications(this);
        Script.Get<MapsManager>().AddTerrainUpdateDelegate(this);
    }

    private void OnDestroy() {
        Script.Get<BuildingManager>()?.EndBuildingNotifications(this);
        Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
    }

    public void SetBlueprint(ConstructionBlueprint blueprint, LayoutCoordinate blueprintLayoutCoordinate) {
        labelText.text = blueprint.label;
        detailText.text = blueprint.description;

        if(blueprint.actionType.HasValue) {
            typeIcon.gameObject.SetActive(true);
            typeIcon.SetActionType(blueprint.actionType.Value);
        } else {
            typeIcon.gameObject.SetActive(false);
        }

        if (blueprint.iconImage != null) {
            icon.gameObject.SetActive(true);
            icon.sprite = blueprint.iconImage;
        }else {
            icon.gameObject.SetActive(false);
        }

        costPanel.SetCost(blueprint.cost);

        this.blueprint = blueprint;
        this.blueprintLayoutCoordinate = blueprintLayoutCoordinate;

        CheckRequirements();
    }

    private void CheckRequirements() {
        if (blueprint == null) {
            return;
        }

        // If we are disabled due to requiredment not being met, use text. If we are disabled from a tutorial standpoint, use nothing
        disabled = blueprint.requirementsMet != null && !blueprint.requirementsMet(blueprintLayoutCoordinate);
        var disabledTextString = disabled ? blueprint.requirementsNotMetString : "";

        var tutorialBlueprint = TutorialManager.isolateUserAction;
        if(tutorialBlueprint != null) {
            disabled = disabled || tutorialBlueprint.blueprint != blueprint;
        }

        if(disabled) {
            canvasGroup.alpha = 0.5f;
            disabledText.text = disabledTextString;
        } else {
            canvasGroup.alpha = 1f;
        }

        if(disabledElement.gameObject.activeSelf != disabled) {
            disabledElement.gameObject.SetActive(disabled);
        }
    }

    /*
     * IPointerClickHandler Interface
     * */

    public void OnPointerClick(PointerEventData eventData) {
        if (disabled) {
            return;
        }

        blueprint.ConstructAt(blueprintLayoutCoordinate);
        Script.Get<UIManager>().PopToRoot();
    }

    /*
     * BuildingsUpdateDelegate Interface
     * */

    public void NewBuildingStarted(Building building) {
        CheckRequirements();
    }

    public void BuildingFinished(Building building) {
        CheckRequirements();
    }

    /*
     * TerrainUpdateDelegate Interface
     * */

    public void NotifyTerrainUpdate(LayoutCoordinate layoutCoordinate) {
        CheckRequirements();
    }
}
