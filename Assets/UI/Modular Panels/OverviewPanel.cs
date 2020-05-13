﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverviewPanel : MonoBehaviour, GameButtonDelegate
{
    private const string SLIDE_OUT = "OverviewSlideOut";
    private const string SLIDE_IN = "OverviewSlideIn";


    [SerializeField]
    private GameButton transitionButton = null;
    [SerializeField]
    private Image transitionButtonIcon = null;
    [SerializeField]
    private Animation anim = null;

    [SerializeField]
    private Sprite openIcon = null;
    [SerializeField]
    private Sprite closeIcon = null;

    private bool minimized = true;

    // Start is called before the first frame update
    void Start()
    {
        transitionButton.buttonDelegate = this;
    }

    /*
     * GameButtonDelegate
     * */

    public void ButtonDidClick(GameButton button) {
        if (minimized) {
            anim.Play(SLIDE_OUT);
            transitionButtonIcon.sprite = closeIcon;
        } else {
            anim.Play(SLIDE_IN);
            transitionButtonIcon.sprite = openIcon;
        }

        minimized = !minimized;
    }
}
