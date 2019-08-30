using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimePanel : MonoBehaviour, TimeUpdateDelegate, PlayerBehaviourUpdateDelegate {
    public Text timeLabel;

    [HideInInspector]
    public TimeSpan currentTime;

    private Color defaultColor;

    private void Awake() {
        currentTime = new TimeSpan();
        defaultColor = timeLabel.color;
    }

    private void Start() {
        DisplayTime();
        Script.Get<TimeManager>().RegisterForTimeUpdateNotifications(this);
        Script.Get<PlayerBehaviour>().RegisterForPlayerBehaviourNotifications(this);
    }

    private void OnDestroy() {
        Script.Get<TimeManager>().EndTimeUpdateNotifications(this);
        Script.Get<PlayerBehaviour>().EndPlayerBehaviourNotifications(this);
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

    /*
     * PlayerBehaviourUpdateDelegate Interface
     * */

    public void PauseStateUpdated(bool paused) {
        timeLabel.color = paused ? ColorSingleton.sharedInstance.disabledRedColor : defaultColor;
    }
}
