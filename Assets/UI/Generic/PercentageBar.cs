using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PercentageBar : TrackingUIElement {

    public Slider sliderBar;
    public Text detailText;

    public enum DisplayType { Bar, Itemized }
    private DisplayType displayType;

    private int required;
    private int has;
    private string unitOfMeasure;

    private void Awake() {
        SetDisplayType(DisplayType.Bar);       
    }

    private void SetDisplayType(DisplayType type) {
        displayType = type;

        switch(displayType) {
            case DisplayType.Bar:                
                sliderBar.gameObject.SetActive(true);
                break;
            case DisplayType.Itemized:
                sliderBar.gameObject.SetActive(false);
                break;
        }
    }

    public void SetPercent(float percent) {
        if (displayType != DisplayType.Bar) {
            SetDisplayType(DisplayType.Bar);
        }

        sliderBar.value = percent;
        detailText.text = "" + Mathf.RoundToInt(percent * 100) + "%";
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
