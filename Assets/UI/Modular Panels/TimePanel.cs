using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimePanel : MonoBehaviour, TimeUpdateDelegate {
    public Text timeLabel;

    [HideInInspector]
    public TimeSpan currentTime;

    private void Awake() {
        currentTime = new TimeSpan();
    }

    private void Start() {
        DisplayTime();
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);
    }

    private void OnDestroy() {
        Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
    }

    private void DisplayTime() {
        timeLabel.text = currentTime.ToString();
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {
        currentTime = currentTime.Add(new TimeSpan(0, 0, 1));
        DisplayTime();
    }
}
