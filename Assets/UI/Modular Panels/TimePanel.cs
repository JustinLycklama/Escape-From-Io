using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimePanel : MonoBehaviour, TimeUpdateDelegate, PlayerBehaviourUpdateDelegate {
    public Text timeLabel;

    private Color defaultColor;

    private TimeManager timeManager;
    private PlayerBehaviour playerBehaviour;

    private void Awake() {
        defaultColor = timeLabel.color;
    }

    private void Start() {
        timeManager = Script.Get<TimeManager>();
        playerBehaviour = Script.Get<PlayerBehaviour>();

        DisplayTime();

        timeManager.RegisterForTimeUpdateNotifications(this);
        playerBehaviour.RegisterForPlayerBehaviourNotifications(this);
    }

    private void OnDestroy() {
        try {
            timeManager.EndTimeUpdateNotifications(this);
            playerBehaviour.EndPlayerBehaviourNotifications(this);
        } catch(System.NullReferenceException e) { }
    }

    private void DisplayTime() {
        timeLabel.text = timeManager.currentDiscreteTime.ToString();
    }

    /*
     * TimeUpdateDelegate Interface
     * */

    public void SecondUpdated() {
        DisplayTime();
    }

    /*
     * PlayerBehaviourUpdateDelegate Interface
     * */

    public void PauseStateUpdated(bool paused) {
        timeLabel.color = paused ? ColorSingleton.sharedInstance.disabledRedColor : defaultColor;
    }
}
