using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlueprintCell : MonoBehaviour, IPointerClickHandler {

    public Image icon;
    public Text labelText;
    public CostPanel costPanel;

    public CanvasGroup canvasGroup;
    public Text disabledText;
    private bool disabled = false;

    private ConstructionBlueprint blueprint;
    private LayoutCoordinate blueprintLayoutCoordinate;

    public void SetBlueprint(ConstructionBlueprint blueprint, LayoutCoordinate blueprintLayoutCoordinate) {
        labelText.text = blueprint.label;
        costPanel.SetCost(blueprint.cost);

        this.blueprint = blueprint;
        this.blueprintLayoutCoordinate = blueprintLayoutCoordinate;

        if (blueprint.requirementsMet != null && !blueprint.requirementsMet(blueprintLayoutCoordinate)) {
            canvasGroup.alpha = 0.5f;
            disabledText.text = blueprint.requirementsNotMetString;
            disabled = true;
        } else {
            disabledText.gameObject.SetActive(false);
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
}
