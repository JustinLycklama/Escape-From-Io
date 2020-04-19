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


    // Pallet

    private static Color RED = new Color(1, 0.08551968f, 0);
    private static Color GREEN = new Color32(0, 217, 94, 255);

    private static Color BRIGHT_YELLOLW = new Color32(252, 236, 3, 255);
    private static Color DARK_ORANGE = new Color32(219, 132, 2, 255);

    // Public

    public Color idleUnitColor = RED;
    public Color inefficientUnitColor = BRIGHT_YELLOLW;
    public Color efficientColor = GREEN;

    private Color durationInitialColor = BRIGHT_YELLOLW;
    private Color durationFinalColor = DARK_ORANGE;

    private Color healthInitialColor = GREEN;
    private Color healthFinalColor = RED;


    public Color disabledRedColor = new Color(1, 0.1686274f, 0.0431372f);

    public Color DurationColorByPercent(float percent) {
        return Color.Lerp(durationFinalColor, durationInitialColor, percent);
    }

    public Color HealthColorByPercent(float percent) {
        return Color.Lerp(healthFinalColor, healthInitialColor, percent);
    }

    //ColorUtility.TryParseHtmlString(hexString, out color);

    //public static class ColorExtensions {
    //    public static Color FromHex(string hexString) {
    //        Color color = Color.white;
    //        ColorUtility.TryParseHtmlString(hexString, out color);

    //        return color;
    //    }
    //}

    //private Color ColorFromHex(this Color c, string hexString) {

    //    Color color = Color.white;
    //    ColorUtility.TryParseHtmlString(hexString, out color);

    //    return color;
    //}

}

