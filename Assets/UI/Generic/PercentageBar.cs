using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PercentageBar : MonoBehaviour/*, TrackingUIInterface*/ {

    public Slider sliderBar;
    public Text detailText;
    public Image fillColorImage;

    public enum DisplayType { Bar, Itemized }
    private DisplayType displayType;

    private int required;
    private int has;
    private string unitOfMeasure;

    // TrackingUIInterface
    public Transform toFollow { get; set; }
    public CanvasGroup canvas;
    public CanvasGroup canvasGroup { get => canvas; }

    private void Awake() {
        SetDisplayType(DisplayType.Bar);
    }

    //private void Update() {
    //    this.UpdateTrackingPosition();
    //}

    private void SetDisplayType(DisplayType type) {
        displayType = type;

        if (sliderBar == null) {
            return;
        }

        switch(displayType) {
            case DisplayType.Bar:                
                sliderBar.gameObject.SetActive(true);
                break;
            case DisplayType.Itemized:
                sliderBar.gameObject.SetActive(false);
                break;
        }
    }

    public void setDetailTextHidden(bool hidden) {
        if (detailText.gameObject.activeSelf != !hidden) {
            detailText.gameObject.SetActive(!hidden);
        }
    }

    public void SetPercent(float percent, string value = null) {
        if (displayType != DisplayType.Bar) {
            SetDisplayType(DisplayType.Bar);
        }

        sliderBar.value = percent;
        if (value != null) {
            detailText.text = value;
        } else {
            detailText.text = "" + Mathf.RoundToInt(percent * 100) + "%";
        }
        
    }

    public void SetRequired(int required, string unitOfMeasure) {
        this.required = required;
        has = 0;
        this.unitOfMeasure = unitOfMeasure;

        SetDisplayType(DisplayType.Itemized);

        UpdateFixedText();
    }

    public void IncrementRequired() {
        has++;
        UpdateFixedText();
    }

    private void UpdateFixedText() {
        detailText.text = has.ToString() + " / " + required.ToString() + " " + unitOfMeasure; 
    }
}
