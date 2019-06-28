using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIConstants
{
    private static UIConstants instance;

    public static UIConstants sharedInstance {
        get {
            if (instance == null) {
                instance = new UIConstants();
            }

            return instance;
        }
    }

    public Color textColor = new Color(199, 232, 255);

    private UIConstants() {

    }
}
