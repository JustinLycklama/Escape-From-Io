using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationItem {
    public string text;

    public NotificationItem(string text) {
        this.text = text;
    }
}

public class NotificationPanel : MonoBehaviour, TableViewDelegate {

    static int notificationDuration = 2;
    static int fadeDuration = 2;

    public TableView tableView;

    List<NotificationItem> notificationItems = new List<NotificationItem>();
    HashSet<NotificationItem> fadingNotifications = new HashSet<NotificationItem>();

    Dictionary<NotificationItem, NotificationItemCell> cellForNotification = new Dictionary<NotificationItem, NotificationItemCell>();

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

    /*
     * TableViewDelegate Interface
     * */

    public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
        NotificationItem notificationItem = notificationItems[row];
        NotificationItemCell notificationCell = cell.GetComponent<NotificationItemCell>();

        notificationCell.text.text = notificationItem.text;
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
