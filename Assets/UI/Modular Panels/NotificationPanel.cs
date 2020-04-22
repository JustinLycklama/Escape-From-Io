using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum NotificationType {
    NewUnit, TaskComplete, UnitBattery, UnitKilled, Warning
}

public class NotificationItem {
    public string text;
    public NotificationType type;
    public MasterGameTask.ActionType? relatedActionType;

    public Transform notificationPosition;

    public NotificationItem(string text, NotificationType type, Transform notificationPosition, MasterGameTask.ActionType? relatedActionType = null) {
        this.text = text;
        this.type = type;
        this.relatedActionType = relatedActionType;

        this.notificationPosition = notificationPosition;
    }
}

[Serializable]
struct NotificationTypeIcon {
    public NotificationType type;
    public Sprite icon;
}

public class NotificationPanel : MonoBehaviour, TableViewDelegate {

    public static int unitDurationWarning = 30;

    static int notificationDuration = 5;
    static int fadeDuration = 2;    

    public TableView tableView;

    List<NotificationItem> notificationItems = new List<NotificationItem>();
    HashSet<NotificationItem> fadingNotifications = new HashSet<NotificationItem>();

    Dictionary<NotificationItem, NotificationItemCell> cellForNotification = new Dictionary<NotificationItem, NotificationItemCell>();

    [SerializeField]
    private List<NotificationTypeIcon> notificationIcons;

    void Awake() {
        tableView.dataDelegate = this;
    }

    public void AddNotification(NotificationItem notificationItem) {

        TimeManager timeManager = Script.Get<TimeManager>();

        notificationItems.Insert(0, notificationItem);

        // Continue the fade
        Action<int, float> continueFadeBlock = (time, percent) => {
            if(cellForNotification.ContainsKey(notificationItem)) {
                cellForNotification[notificationItem].canvasGroup.alpha = 1 - percent;
            }
        };

        // Complete the fade
        Action completeFadeBlock = () => {
            notificationItems.Remove(notificationItem);
            fadingNotifications.Remove(notificationItem);
            tableView.ReloadData();
        };

        // Begin to fade
        Action startFadeBlock = () => {
            fadingNotifications.Add(notificationItem);
            tableView.ReloadData();

            timeManager.AddNewTimer(fadeDuration, continueFadeBlock, completeFadeBlock, 2);
        };

        timeManager.AddNewTimer(notificationDuration, null, startFadeBlock);
        tableView.ReloadData();
    }

    public Sprite IconForNotificationType(NotificationType type) {

        foreach (NotificationTypeIcon typeIcon in notificationIcons) {
            if (typeIcon.type == type) {
                return typeIcon.icon;
            }
        }

        return null;
    }

    /*
     * TableViewDelegate Interface
     * */

    public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
        NotificationItem notificationItem = notificationItems[row];
        NotificationItemCell notificationCell = cell.GetComponent<NotificationItemCell>();

        notificationCell.SetNotification(notificationItem);
        cellForNotification.Remove(notificationItem);

        if(fadingNotifications.Contains(notificationItem)) {
            cellForNotification.Add(notificationItem, notificationCell);
        } else {
            notificationCell.canvasGroup.alpha = 1;
        }
    }

    public int NumberOfRows(TableView table) {
        return notificationItems.Count;
    }
}
