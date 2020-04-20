using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverviewPanel : MonoBehaviour, GameButtonDelegate
{
    private const string SLIDE_OUT = "OverviewSlideOut";
    private const string SLIDE_IN = "OverviewSlideIn";


    [SerializeField]
    private GameButton transitionButton;
    [SerializeField]
    private Image transitionButtonIcon;
    [SerializeField]
    private Animation anim;

    [SerializeField]
    private Sprite openIcon;
    [SerializeField]
    private Sprite closeIcon;

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
