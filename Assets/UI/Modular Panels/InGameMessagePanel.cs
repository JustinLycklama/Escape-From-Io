using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameMessagePanel : MonoBehaviour
{
    [SerializeField]
    private Text title;
    [SerializeField]
    private Text contents;

    [SerializeField]
    public GameButton continueButton;

    public void SetTitleAndText(string titleText, string message) {
        title?.gameObject.SetActive(titleText != null);

        if (title != null) {
            title.text = titleText;
        }

        contents.text = message;
    }

}
