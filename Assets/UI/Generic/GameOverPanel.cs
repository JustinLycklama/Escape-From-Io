using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : FadePanel
{
    public GameButton continueButton;

    [SerializeField]
    private GameObject failureText;
    [SerializeField]
    private GameObject successText;

    [SerializeField]
    private GameObject failureTitle;
    [SerializeField]
    private GameObject successTitle;

    [SerializeField]
    private Image backgroundImage;
    [SerializeField]
    private Sprite successImage;

    public void SetSuccess() {
        failureText.SetActive(false);
        failureTitle.SetActive(false);

        successText.SetActive(true);
        successTitle.SetActive(true);

        backgroundImage.sprite = successImage;
    }
}
