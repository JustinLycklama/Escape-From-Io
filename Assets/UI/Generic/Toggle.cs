using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toggle : GameButton {

    public Sprite off;
    public Sprite offMouseOver;
    public Sprite offClick;

    private Sprite originalSprite;
    private Sprite originalMouseOver;
    private Sprite originalClick;

    public bool state;

    protected override void Awake() {
        base.Awake();

        originalSprite = original;
        originalMouseOver = mouseOver;
        originalClick = click;

        SetState(state);
    }    

    public void SetState(bool toggled) {

        state = toggled;

        if (image == null) {
            return;
        }

        if (toggled) {
            original = originalSprite;
            mouseOver = originalMouseOver;
            click = originalClick;
        } else {
            original = off;
            mouseOver = offMouseOver;
            click = offClick;
        }

        image.sprite = click;           
    }
}
