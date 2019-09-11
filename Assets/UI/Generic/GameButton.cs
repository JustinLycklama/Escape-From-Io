using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface GameButtonDelegate {
    void ButtonDidClick(GameButton button);
}

public class GameButton : Clickable {

    public GameButtonDelegate buttonDelegate;

    protected override void DidClick() {
        if (buttonDelegate != null) {
            buttonDelegate.ButtonDidClick(this);
        }
    }
}
