using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum NotificationType {
    NewUnit, NewEnemy, TaskComplete, UnitBattery, UnitKilled, EnemyKilled, Warning
}

public class NotificationItem {
    public string text;
    public NotificationType type;
    public MasterGameTask.ActionType? relatedActionType;

    public Transform notificationPosition;

    public bool isNew;

    public NotificationItem(string text, NotificationType type, Transform notificationPosition, MasterGameTask.ActionType? relatedActionType = null) {
        this.text = text;
        this.type = type;
        this.relatedActionType = relatedActionType;

        this.notificationPosition = notificationPosition;

        isNew = true;
    }
}

[Serializable]
struct NotificationTypeVFX {
    public NotificationType type;
    public GameObject vfxPrefab;
}

public class NotificationPanel : MonoBehaviour, TableViewDelegate {

    public static int unitDurationWarning = 30;

    private const float notificationDelay = 2f;

    private const int notificationDuration = 5;
    private const int fadeDuration = 2;    

    [SerializeField]
    private List<NotificationTypeVFX> notificationVFX;

    private bool supressNotifications = false;

    private TimeManager timeManager;

    private Queue<NotificationItem> newNotificationQueue = new Queue<NotificationItem>();

    // Tableview Info
    public TableView tableView;

    private List<NotificationItem> notificationItems = new List<NotificationItem>();
    private HashSet<NotificationItem> fadingNotifications = new HashSet<NotificationItem>();

    private Dictionary<NotificationItem, NotificationItemCell> cellForNotification = new Dictionary<NotificationItem, NotificationItemCell>();

    void Awake() {
        tableView.dataDelegate = this;
    }

    private void Start() {
        timeManager = Script.Get<TimeManager>();

        StartCoroutine(UnravelNotificationQueue());
    }

    private IEnumerator UnravelNotificationQueue() {
        while(true) {

            yield return new WaitForSeconds(notificationDelay);

            if (newNotificationQueue.Count == 0) {
                continue;
            }

            NotificationItem notificationItem = newNotificationQueue.Dequeue();
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
    }

    public void SetSupressNotifications(bool state) {
        supressNotifications = state;
    }

    public void AddNotification(NotificationItem notificationItem) {
        if (supressNotifications) {
            return;
        }

        newNotificationQueue.Enqueue(notificationItem);
    }

    public GameObject VFXForNotificationType(NotificationType type) {

        foreach (NotificationTypeVFX typeVFX in notificationVFX) {
            if (typeVFX.type == type) {
                return typeVFX.vfxPrefab;
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
