using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlueprintCell : MonoBehaviour, IPointerClickHandler, BuildingsUpdateDelegate, TerrainUpdateDelegate {

    public Image icon;
    public Text labelText;
    public Text detailText;

    public CostPanel costPanel;

    public CanvasGroup canvasGroup;
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

        costPanel.SetCost(blueprint.cost);

        this.blueprint = blueprint;
        this.blueprintLayoutCoordinate = blueprintLayoutCoordinate;

        CheckRequirements();
    }

    private void CheckRequirements() {
        if (blueprint == null) {
            return;
        }

        disabled = false;
        if(blueprint.requirementsMet != null && !blueprint.requirementsMet(blueprintLayoutCoordinate)) {
            canvasGroup.alpha = 0.5f;
            disabledText.text = blueprint.requirementsNotMetString;
            disabled = true;
        } else {
            canvasGroup.alpha = 1f;
        }

        if(disabledText.gameObject.activeSelf != disabled) {
            disabledText.gameObject.SetActive(disabled);
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
