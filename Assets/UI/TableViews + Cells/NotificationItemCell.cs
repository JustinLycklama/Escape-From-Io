using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationItemCell : MonoBehaviour, GameButtonDelegate {
    public CanvasGroup canvasGroup;

    [SerializeField]
    private Text text = null;
    [SerializeField]
    private GameButton gameButton = null;

    //[SerializeField]
    //private Image typeIcon;
    [SerializeField]
    private UnitTypeIcon unitTypeIcon = null;

    [SerializeField]
    private Transform vfxParent = null;
    [SerializeField]
    private Transform vfxPosition = null;

    private GameObject vfxPrefabInstance;

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
        vfxPosition.position = new Vector3(vfxPosition.position.x, vfxPosition.position.y, -5);
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

        if(notificationItem.isNew) {
            GameObject vfxPrefab = notificationPanel.VFXForNotificationType(notificationItem.type);

            if(vfxPrefabInstance == null && vfxPrefab != null) {
                vfxPrefabInstance = Instantiate(vfxPrefab, vfxPosition.position, Quaternion.identity, vfxParent);
            }
        }        
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        //if(notificationItem != null && notificationItem.notificationPosition != null)
        //    Script.Get<PlayerBehaviour>().PanCameraToPosition(notificationItem.notificationPosition.position);
    }
}
