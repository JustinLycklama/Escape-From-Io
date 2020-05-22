using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : FadePanel
{
    public GameButton continueButton;

    [SerializeField]
    private GameObject failureText = null;
    [SerializeField]
    private GameObject successText = null;

    [SerializeField]
    private GameObject failureTitle = null;
    [SerializeField]
    private GameObject successTitle = null;

    [SerializeField]
    private Image backgroundImage = null;
    [SerializeField]
    private Sprite successImage = null;

    private void Start() {
        background.raycastTarget = false;
    }

    public void SetSuccess() {
        failureText.SetActive(false);
        failureTitle.SetActive(false);

        successText.SetActive(true);
        successTitle.SetActive(true);

        backgroundImage.sprite = successImage;
    }

    public override void FadeOut(bool fadeOut, bool displayPercent, Action completed) {
        base.FadeOut(fadeOut, displayPercent, completed);

        continueButton.image.raycastTarget = fadeOut;
    }
}
