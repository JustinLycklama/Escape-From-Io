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

    protected override void RaycastTarget(bool state) {
        base.RaycastTarget(state);

        continueButton.image.raycastTarget = state;
    }

    public void SetSuccess() {
        failureText.SetActive(false);
        failureTitle.SetActive(false);

        successText.SetActive(true);
        successTitle.SetActive(true);

        backgroundImage.sprite = successImage;
    }
}
