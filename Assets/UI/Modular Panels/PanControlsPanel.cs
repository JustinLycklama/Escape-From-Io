using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanControlsPanel : MonoBehaviour
{
    public Joystick joystick;
    public Image wasdImage;

    public void SetJoystickEnabled(bool enabled) {
        if (enabled) {            
            wasdImage.gameObject.SetActive(false);
        }
        else {
            joystick.gameObject.SetActive(false);

            if (!TutorialManager.isTutorial) {
                wasdImage.gameObject.SetActive(false);
            }
        }
    }
}
