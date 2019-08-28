using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSingleton {

    private static ColorSingleton backingInstance;

    public static ColorSingleton sharedInstance {
        get {

            if (backingInstance == null) {
                backingInstance = new ColorSingleton();
            }

            return backingInstance;
        }
    }


    public Color startDurationColor = new Color(0, 1, 0.572549f);
    public Color endDurationColor = new Color(1, 0.08551968f, 0);

    public Color GreenToRedByPercent(float percent) {
        return Color.Lerp(endDurationColor, startDurationColor, percent);
    }
}
