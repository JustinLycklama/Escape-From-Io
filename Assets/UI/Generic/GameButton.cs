using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ButtonDelegate {
    void ButtonDidClick(GameButton button);
}

public class GameButton : Clickable {

    public ButtonDelegate buttonDelegate;

    protected override void DidClick() {
        if (buttonDelegate != null) {
            buttonDelegate.ButtonDidClick(this);
        }
    }
}
