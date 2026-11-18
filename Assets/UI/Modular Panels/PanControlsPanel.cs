using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shows the WASD pan hint during the tutorial
public class PanControlsPanel : MonoBehaviour
{
    public Image wasdImage;

    public void UpdateHintVisibility() {
        wasdImage.gameObject.SetActive(TutorialManager.isTutorial);
    }
}
