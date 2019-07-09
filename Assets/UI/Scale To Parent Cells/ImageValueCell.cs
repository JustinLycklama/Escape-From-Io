using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageValueCell : MonoBehaviour {
    public Image image;
    public Text value;

    private void Awake() {
        Image background = GetComponent<Image>();
        Color bgColor = background.color;
        bgColor.a = 0.5f;
        background.color = bgColor;
    }
}
