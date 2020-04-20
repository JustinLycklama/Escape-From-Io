using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitTypeIcon : MonoBehaviour
{
    [SerializeField]
    private MasterGameTask.ActionType actionType;

    [SerializeField]
    private Image icon;

    private void Start() {
        SetActionType(actionType);
    }

    public void SetActionType(MasterGameTask.ActionType actionType) {
        this.actionType = actionType;

        icon.sprite = Script.Get<UnitManager>().UnitIconForActionType(actionType);
    }

    public void SetEnabled(bool enabled) {
        icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, enabled ? 1.0f : 0.25f);
    }
}
