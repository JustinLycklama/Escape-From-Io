﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MasterAndGameTaskCell : MonoBehaviour {

    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Text masterTaskDescription;

    [SerializeField]
    private Text gameTaskDescription;
    [SerializeField]
    private GameObject gameTaskContainer;

    [SerializeField]
    private Sprite blackAndWhiteImage;



    [SerializeField]
    private Color blockingColor;

    [SerializeField]
    private Color highlightColor;

    [SerializeField]
    private Image highlightArea;

    MasterGameTask task;
    GameTask gameTask;

    public string defaultText = "No Task"; 

    private void Awake() {
        masterTaskDescription.text = defaultText;
        gameTaskDescription.text = "";

        gameTaskContainer.SetActive(false);
    }


    public void SetTask(MasterGameTask task, GameTask gameTask) {

        this.task = task;
        this.gameTask = gameTask;

        string masterTaskText = task?.description;
        string gameTaskText = null;

        if(task != null && task.blockerTasks.Count > 0) {
            masterTaskText = task.blockerTasks[0].description;
            gameTaskText = "Blocking Task";

            highlightArea.color = blockingColor;
        } else {
            masterTaskText = task?.description;
            gameTaskText = gameTask?.description;

            highlightArea.color = highlightColor;
        }

        masterTaskDescription.text = masterTaskText ?? defaultText;
        gameTaskDescription.text = gameTaskText ?? "";

        var gameTaskDescriptionEnabled = gameTaskText != null;
        if(gameTaskContainer.activeSelf != gameTaskDescriptionEnabled) {
            gameTaskContainer.SetActive(gameTaskDescriptionEnabled);
        }
    }

    public void SetBlackAndWhite() {
        backgroundImage.sprite = blackAndWhiteImage;
    }

    public void Colorize(Color color) {
        backgroundImage.color = color;
    }
}
