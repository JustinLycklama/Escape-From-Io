using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface HelpPresenter {
    void dismiss(HelpWindow window);
}

public class HelpWindow : MonoBehaviour, GameButtonDelegate {
    public GameButton closeButton;
    public FadePanel fadePanel;

    [HideInInspector]
    public HelpPresenter presenter;

    private void Awake() {
        closeButton.buttonDelegate = this;
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        presenter.dismiss(this);     
    }
}
