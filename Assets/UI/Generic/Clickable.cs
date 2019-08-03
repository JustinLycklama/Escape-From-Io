using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Clickable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {
    public Sprite mouseOver;
    public Sprite click;

    protected Image image;
    protected Sprite original;

    protected virtual void Awake() {
        image = GetComponent<Image>();
        original = image.sprite;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        image.sprite = mouseOver;
    }

    public void OnPointerExit(PointerEventData eventData) {
        image.sprite = original;
    }
   
    public void OnPointerDown(PointerEventData eventData) {
        image.sprite = click;
    }

    public void OnPointerUp(PointerEventData eventData) {
        image.sprite = original;
    }

    protected abstract void DidClick();

    public void OnPointerClick(PointerEventData eventData) {
        DidClick();
    }
}
