using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageValueCell : MonoBehaviour {
    public Image image;
    public Text value;

    int integerValue = 0;

    private void Awake() {
        Image background = GetComponent<Image>();
        Color bgColor = background.color;
        bgColor.a = 0.5f;
        background.color = bgColor;
    }

    public void SetIntegerValue(int intValue) {
        integerValue = intValue;
        value.text = integerValue.ToString();
    }

    public void Increment() {
        SetIntegerValue(integerValue + 1);
    }

    public void Decrement() {
        SetIntegerValue(integerValue - 1);
    }
}
