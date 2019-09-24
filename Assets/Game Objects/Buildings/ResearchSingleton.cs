using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchSingleton {
    public static ResearchSingleton sharedInstance = new ResearchSingleton();

    public float unitSpeedMultiplier = 1;
    public float unitActionMultiplier = 1;

    public int unitDurationAddition = 0;
    public int visionRadiusAddiiton = 0;
}
