using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchSingleton: SceneChangeListener {
    private static ResearchSingleton backingInstance;
    public static ResearchSingleton sharedInstance {
        get {
            if (backingInstance == null) {
                backingInstance = new ResearchSingleton();
            }

            return backingInstance;
        }
    }

    public float unitSpeedMultiplier;
    public float unitActionMultiplier;

    public int unitDurationAddition;
    public int visionRadiusAddiiton;

    Dictionary<Type, int> buildingCount = new Dictionary<Type, int>();

    private ResearchSingleton() {
        ResetResearch();

        SceneManagement.sharedInstance.RegisterForSceneUpdates(this);
    }
    
    private void ResetResearch() {
        unitSpeedMultiplier = 1;
        unitActionMultiplier = 1;

        unitDurationAddition = 0;
        visionRadiusAddiiton = 0;

        buildingCount.Clear();
    }

    public void AddBuildingCount(Building building) {
        Type type = building.GetType();
        buildingCount[type] = 1;
    }

    public void RemoveBuildingCount(Building building) {
        Type type = building.GetType();

        buildingCount[type] = 0;
    }

    public int GetBuildingCount(Type type) {
        if (!buildingCount.ContainsKey(type)) {
            return 0;
        }

        return buildingCount[type];
    }

    /*
     * SceneChangeListener Interface
     * */

    public void WillSwitchScene() {
        ResetResearch();
    }
}
