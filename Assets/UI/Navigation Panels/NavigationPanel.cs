using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavigationPanel : MonoBehaviour, GameButtonDelegate
{
    [HideInInspector]
    public NavigationPanel backTrace;
    [HideInInspector]
    public Transform positionParent;

    //private GameObject defaultBackButtonObject;

    [SerializeField]
    private GameButton backButton = null;

    private UIManager uIManager;

    protected virtual void Awake() {
        if (positionParent == null) {
            positionParent = transform.parent; // Tag.UIArea.GetGameObject().transform;
        }
    }

    protected virtual void Start() {
        uIManager = Script.Get<UIManager>();

        /*if(backButton == null) {
            var defaultBackButtonObject = new GameObject("BackButton");
            defaultBackButtonObject.transform.SetParent(transform);

            backButton = defaultBackButtonObject.AddComponent<GameButton>();

            LayoutElement layoutElement = defaultBackButtonObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            RectTransform rectTransform = defaultBackButtonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);

            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.sizeDelta = new Vector2(50, 33);

            Text text = defaultBackButtonObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            text.text = "Back";
            text.alignment = TextAnchor.MiddleCenter;
        }*/

        if(backButton != null) {
            backButton.buttonDelegate = this;
        }        
    }

    private void OnEnable() {
        if (backTrace == null && backButton != null ) {
            backButton.gameObject.SetActive(false);
        }
    }

    public void PushOntoStackFrom(NavigationPanel previousPanel) {
        backTrace = previousPanel;
        positionParent = previousPanel.positionParent;

        previousPanel.gameObject.SetActive(false);
        transform.SetParent(positionParent, false);

        backButton?.gameObject.SetActive(true);
    }

    // Returns panel we are popping to
    public NavigationPanel PopFromStack() {
        backTrace.gameObject.SetActive(true);

        gameObject.transform.SetParent(null);
        Destroy(gameObject);

        return backTrace;
    }

    /*
     * GameButtonDelegate
     * */

    public virtual void ButtonDidClick(GameButton button) {
        if (button == backButton) {
            uIManager.Pop();
        }        
    }
}
