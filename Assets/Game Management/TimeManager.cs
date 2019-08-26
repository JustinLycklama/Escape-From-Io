using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour {

    class TimeUpdateObject {
        public int totalDuration;
        public float currentTime;

        public int updateFrequencyComparatorPower;

        public Action<int, float> updateBlock;
        public Action completionBlock;
    }

    HashSet<TimeUpdateObject> timeObjects = new HashSet<TimeUpdateObject>();

    //void Start() {
    //    StartCoroutine(UpdateTimers());
    //}

    //IEnumerator UpdateTimers() {
    //    while(true) {
    //        foreach(TimeUpdateObject timeObject in new HashSet<TimeUpdateObject>(timeObjects)) {

    //            int oldTime = Mathf.FloorToInt(timeObject.currentTime);
    //            timeObject.currentTime += Time.deltaTime;

    //            int newTime = Mathf.FloorToInt(timeObject.currentTime);

    //            if(newTime > timeObject.totalDuration) {
    //                timeObject.completionBlock();
    //                timeObjects.Remove(timeObject);
    //            } else if(oldTime != newTime) {
    //                timeObject.updateBlock((timeObject.totalDuration - newTime), (timeObject.currentTime / (float)timeObject.totalDuration));
    //            }
    //        }

    //        yield return new WaitForSeconds(0.75f);
    //    }      
    //}

    void Update() {
        foreach(TimeUpdateObject timeObject in new HashSet<TimeUpdateObject>(timeObjects)) {

            float powerMultiplier = Mathf.Pow(10f, timeObject.updateFrequencyComparatorPower);

            int oldTimeComparison = Mathf.FloorToInt(timeObject.currentTime * powerMultiplier);
            timeObject.currentTime += Time.deltaTime;

            int newTime = Mathf.FloorToInt(timeObject.currentTime);
            int newTimeComparison = Mathf.FloorToInt(timeObject.currentTime * powerMultiplier);

            if(newTime > timeObject.totalDuration) {
                timeObject.completionBlock?.Invoke();
                timeObjects.Remove(timeObject);
            } else if(oldTimeComparison != newTimeComparison) {
                int timeLeft = (timeObject.totalDuration - newTime);                

                timeObject.updateBlock?.Invoke(timeLeft, (timeObject.currentTime / timeObject.totalDuration));
            }
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
}
