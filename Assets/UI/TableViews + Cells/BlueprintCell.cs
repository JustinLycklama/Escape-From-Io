using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlueprintCell : MonoBehaviour, IPointerClickHandler {

    public Image icon;
    public Text labelText;
    public CostPanel costPanel;

    private ConstructionBlueprint blueprint;
    private LayoutCoordinate blueprintLayoutCoordinate;

    public void SetBlueprint(ConstructionBlueprint blueprint, LayoutCoordinate blueprintLayoutCoordinate) {
        labelText.text = blueprint.label;
        costPanel.SetCost(blueprint.cost);

        this.blueprint = blueprint;
        this.blueprintLayoutCoordinate = blueprintLayoutCoordinate;
    }

    /*
     * IPointerClickHandler Interface
     * */

    public void OnPointerClick(PointerEventData eventData) {
        blueprint.ConstructAt(blueprintLayoutCoordinate);
        Script.Get<UIManager>().PopToRoot();
    }
}
