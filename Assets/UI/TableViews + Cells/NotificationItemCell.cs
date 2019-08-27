using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationItemCell : MonoBehaviour, ButtonDelegate {
    public CanvasGroup canvasGroup;

    public Text text;
    public GameButton gameButton;

    private NotificationItem notificationItem;

    private void Awake() {
        gameButton.buttonDelegate = this;
    }

    public void SetNotification(NotificationItem notificationItem) {
        this.notificationItem = notificationItem;
        text.text = notificationItem.text;
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if(notificationItem != null && notificationItem.notificationPosition != null)
            Script.Get<PlayerBehaviour>().JumpCameraToPosition(notificationItem.notificationPosition.position);
    }
}
