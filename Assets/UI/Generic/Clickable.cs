using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Clickable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {
    public Sprite mouseOver;
    public Sprite click;

    public Image image { get; protected set; }
    protected Sprite original;

    public bool buttonEnabled { get; private set; }
    public bool hoverLock { get; private set; }

    private CanvasGroup canvasGroup;

    protected virtual void Awake() {
        image = GetComponent<Image>();
        original = image.sprite;

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        SetEnabled(enabled);
        SetHoverLock(hoverLock);
    }

    public void SetEnabled(bool enabled) {
        buttonEnabled = enabled;
        image.sprite = original;

        if (canvasGroup != null) {
            canvasGroup.alpha = enabled ? 1f : 0.25f;
        }        
    }

    public void SetHoverLock(bool hoverLock) {
        this.hoverLock = hoverLock;

        image.sprite = hoverLock ? mouseOver : original;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!buttonEnabled) { return; }
        image.sprite = mouseOver;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if(!buttonEnabled) { return; }
        image.sprite = click;
    }

    public void OnPointerExit(PointerEventData eventData) {
        if(!buttonEnabled) { return; }
        image.sprite = hoverLock ? mouseOver : original;
    }   

    public void OnPointerUp(PointerEventData eventData) {
        if(!buttonEnabled) { return; }
        image.sprite = hoverLock ? mouseOver : original; ;
    }

    protected abstract void DidClick();

    public void OnPointerClick(PointerEventData eventData) {
        if(!buttonEnabled) { return; }
        DidClick();
    }
}
