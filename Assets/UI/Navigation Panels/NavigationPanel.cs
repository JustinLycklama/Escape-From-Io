﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavigationPanel : MonoBehaviour
{
    [HideInInspector]
    public NavigationPanel backTrace;
    [HideInInspector]
    public Transform positionParent;

    GameObject backButtonObject;

    protected virtual void Awake() {
        if (positionParent == null) {
            positionParent = transform.parent; // Tag.UIArea.GetGameObject().transform;
        }

        backButtonObject = new GameObject("BackButton");
        backButtonObject.transform.SetParent(transform);
        //backButtonObject.transform.

        Button backButton = backButtonObject.AddComponent<Button>();
        backButton.onClick.AddListener(Script.Get<UIManager>().Pop);

        Text text = backButtonObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        text.text = "Back";
        text.alignment = TextAnchor.MiddleCenter;

        LayoutElement layoutElement = backButtonObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        RectTransform rectTransform = backButtonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);

        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.sizeDelta = new Vector2(50, 33);

        backButtonObject.SetActive(false);
    }

    private void OnEnable() {
        if (backTrace == null && backButtonObject != null ) {
            backButtonObject.SetActive(false);
        }
    }

    public void PushOntoStackFrom(NavigationPanel previousPanel) {
        backTrace = previousPanel;
        positionParent = previousPanel.positionParent;

        previousPanel.gameObject.SetActive(false);
        transform.SetParent(positionParent, false);

        backButtonObject.SetActive(true);
    }

    // Returns panel we are popping to
    public NavigationPanel PopFromStack() {
        backTrace.gameObject.SetActive(true);

        gameObject.transform.SetParent(null);
        Destroy(gameObject);

        return backTrace;
    }
}
