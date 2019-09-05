using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadePanel : MonoBehaviour
{
    public Image background;
    public PercentageBar percentBar;

    private float fadeSpeed = 1.5f;

    private void Awake() {
        DisplayPercentBar(false);
    }

    public void DisplayPercentBar(bool visible) {
        if(percentBar.gameObject.activeSelf != visible) {
            percentBar.gameObject.SetActive(visible);
        }
    }

    public void SetPercent(float percent) {
        percentBar.SetPercent(percent);
    }

    public void FadeOut(bool fadeOut, Action completed) {
        DisplayPercentBar(false);
        background.raycastTarget = true;
        StartCoroutine(Fade(fadeOut, completed));
    }

    IEnumerator Fade(bool fadeOut, Action completed) {

        Color baseFadeColor = background.color;
        float alpha = baseFadeColor.a;

        int success = fadeOut ? 1 : 0;
        while(alpha != success) {

            alpha += fadeSpeed * Time.deltaTime * (fadeOut ? 1 : -1);

            if(fadeOut && alpha > 1) {
                alpha = 1;                
            }

            if(!fadeOut && alpha < 0) {
                alpha = 0;
            }

            baseFadeColor.a = alpha;
            background.color = baseFadeColor;

            yield return null;
        }

        completed?.Invoke();
        background.raycastTarget = false;
    }

}
