using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadePanel : MonoBehaviour
{
    public Image background;
    public CanvasGroup canvasGroup;
    public PercentageBar percentBar;

    public float fadeSpeed = 1.5f;

    private void Awake() {
        percentBar?.SetPercent(0);
    }

    private void DisplayPercentBar(bool visible) {
        if(percentBar?.gameObject.activeSelf != visible) {
            percentBar?.gameObject.SetActive(visible);
        }
    }

    public void SetPercent(float percent) {
        percentBar?.SetPercent(percent);
    }

    public void FadeOut(bool fadeOut, bool displayPercent, Action completed) {
        DisplayPercentBar(displayPercent);

        RaycastTarget(true);
        StartCoroutine(Fade(fadeOut, completed));
    }

    protected virtual void RaycastTarget(bool state) {
        background.raycastTarget = state;
    }

    IEnumerator Fade(bool fadeOut, Action completed) {

        float alpha = canvasGroup.alpha;

        int success = fadeOut ? 1 : 0;
        while(alpha != success) {

            alpha += fadeSpeed * Time.deltaTime * (fadeOut ? 1 : -1);

            if(fadeOut && alpha > 1) {
                alpha = 1;                
            }

            if(!fadeOut && alpha < 0) {
                alpha = 0;
            }

            canvasGroup.alpha = alpha;

            yield return null;
        }

        completed?.Invoke();
        RaycastTarget(false);
    }
}
