using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitTypeIcon : MonoBehaviour
{
    [SerializeField]
    private MasterGameTask.ActionType actionType;

    [SerializeField]
    private Unit.FactionType factionType;

    [SerializeField]
    private Image icon;

    private void Start() {
        SetActionType(actionType, factionType);
    }

    public void SetActionType(MasterGameTask.ActionType actionType, Unit.FactionType factionType = Unit.FactionType.Player) {
        this.actionType = actionType;
        this.factionType = factionType;

        UnitManager unitManager = Script.Get<UnitManager>();

        if (factionType == Unit.FactionType.Enemy) {
            icon.sprite = unitManager.enemyIcon;
        } else {
            icon.sprite = unitManager.UnitIconForActionType(actionType);
        }
    }

    public void SetEnabled(bool enabled) {
        icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, enabled ? 1.0f : 0.25f);
    }
}
