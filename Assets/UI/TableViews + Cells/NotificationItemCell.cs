using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationItemCell : MonoBehaviour, GameButtonDelegate {
    public CanvasGroup canvasGroup;

    [SerializeField]
    private Text text;
    [SerializeField]
    private GameButton gameButton;

    [SerializeField]
    private Image typeIcon;
    [SerializeField]
    private UnitTypeIcon unitTypeIcon;

    private NotificationItem notificationItem;

    private NotificationPanel _notPanel;
    private NotificationPanel notificationPanel {
        get {
            if (_notPanel == null) {
                _notPanel = Script.Get<NotificationPanel>();
            }

            return _notPanel;
        }
    }


    private void Start() {
        gameButton.buttonDelegate = this;
    }

    public void SetNotification(NotificationItem notificationItem) {
        this.notificationItem = notificationItem;
        text.text = notificationItem.text;

        var activeUnitTypeIcon = (notificationItem.relatedActionType != null);
        if (unitTypeIcon.gameObject.activeSelf != activeUnitTypeIcon) {
            unitTypeIcon.gameObject.SetActive(activeUnitTypeIcon);
        }

        if (activeUnitTypeIcon) {
            unitTypeIcon.SetActionType(notificationItem.relatedActionType.Value);
        }

        typeIcon.sprite = notificationPanel.IconForNotificationType(notificationItem.type);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if(notificationItem != null && notificationItem.notificationPosition != null)
            Script.Get<PlayerBehaviour>().PanCameraToPosition(notificationItem.notificationPosition.position);
    }
}
