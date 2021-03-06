﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TimeUpdateDelegate {
    void SecondUpdated();
}


public class TimeManager : MonoBehaviour, PlayerBehaviourUpdateDelegate {

    class TimeUpdateObject {
        public int totalDuration;
        public float currentTime;

        public int updateFrequencyComparatorPower;

        public Action<int, float> updateBlock;
        public Action completionBlock;
    }

    [HideInInspector]
    public float globalTimer { get; private set; }

    [HideInInspector]
    public TimeSpan currentDiscreteTime { get; private set; }

    // Update Blocks
    HashSet<TimeUpdateObject> timeObjects = new HashSet<TimeUpdateObject>();

    // Interface Delegates
    List<TimeUpdateDelegate> delegateList = new List<TimeUpdateDelegate>();

    private bool skipTimeUpdate = false;

    private void Start() {
        globalTimer = Time.time;
        currentDiscreteTime = new TimeSpan();

        Script.Get<PlayerBehaviour>().RegisterForPlayerBehaviourNotifications(this);
    }

    private void OnDestroy() {
        try {
            Script.Get<PlayerBehaviour>().EndPlayerBehaviourNotifications(this);
        } catch(NullReferenceException) { }
    }

    void Update() {

        if (skipTimeUpdate) {
            return;
        }

        // Interface Updates
        int oldGlobalComparison = Mathf.FloorToInt(globalTimer);
        globalTimer += Time.deltaTime;
        int newGlobalComparison = Mathf.FloorToInt(globalTimer);

        if (oldGlobalComparison != newGlobalComparison) {
            currentDiscreteTime = currentDiscreteTime.Add(new TimeSpan(0, 0, 1));

            foreach(TimeUpdateDelegate updateDelegate in delegateList) {
                updateDelegate.SecondUpdated();
            }
        }        

        // Update Blocks
        foreach(TimeUpdateObject timeObject in new HashSet<TimeUpdateObject>(timeObjects)) {

            float powerMultiplier = Mathf.Pow(10f, timeObject.updateFrequencyComparatorPower);

            int oldTimeComparison = Mathf.FloorToInt(timeObject.currentTime * powerMultiplier);
            timeObject.currentTime += Time.deltaTime;

            int newTime = Mathf.FloorToInt(timeObject.currentTime);
            int newTimeComparison = Mathf.FloorToInt(timeObject.currentTime * powerMultiplier);

            if(newTime >= timeObject.totalDuration) {
                timeObject.completionBlock?.Invoke();
                timeObjects.Remove(timeObject);
            } else if(oldTimeComparison != newTimeComparison) {
                int timeLeft = (timeObject.totalDuration - newTime);                

                timeObject.updateBlock?.Invoke(timeLeft, (timeObject.currentTime / timeObject.totalDuration));
            }
        }
    }

    public void SimulateSecondsUpdated() {
        foreach(TimeUpdateDelegate updateDelegate in delegateList) {
            updateDelegate.SecondUpdated();
        }
    }

    public void AddNewTimer(int duration, Action<int, float> updateBlock, Action completionBlock, int comparisonPower = 0) {
        TimeUpdateObject newObject = new TimeUpdateObject();

        newObject.totalDuration = duration;
        newObject.currentTime = 0;

        newObject.updateFrequencyComparatorPower = comparisonPower;

        newObject.updateBlock = updateBlock;
        newObject.completionBlock = completionBlock;

        timeObjects.Add(newObject);
    }

    /*
     * TimeUpdateDelegate Methods
     * */

    public void RegisterForTimeUpdateNotifications(TimeUpdateDelegate updateDelegate) {
        delegateList.Add(updateDelegate);        
    }

    public void EndTimeUpdateNotifications(TimeUpdateDelegate updateDelegate) {
        delegateList.Remove(updateDelegate);
    }

    /*
     * PlayerBehaviourUpdateDelegate Interface
     * */

    public void PauseStateUpdated(bool paused) {
        skipTimeUpdate = paused;
    }    
}
