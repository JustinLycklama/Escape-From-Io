using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour, GameButtonDelegate
{
    private PlayerBehaviour playerBehaviour;

    [SerializeField]
    private InGameMessagePanel majorPanel;
    [SerializeField]
    private InGameMessagePanel minorPanel;

    Action currentMajorMessageAction = null;
    Queue<(string, string, Action)> nextMinorMessagesQueue = new Queue<(string, string, Action)>();

    private void Awake() {
        majorPanel.gameObject.SetActive(false);
        minorPanel.gameObject.SetActive(false);

        playerBehaviour = Script.Get<PlayerBehaviour>();

        majorPanel.continueButton.buttonDelegate = this;
        minorPanel.continueButton.buttonDelegate = this;
    }

    public void SetMajorMessage(string title, string message, Action onComplete) {
        majorPanel.gameObject.SetActive(true);
        majorPanel.SetTitleAndText(title, message);

        playerBehaviour.SetInternalPause(true);

        currentMajorMessageAction = onComplete;
    }

    public void EnqueueMessage(string title, string message, Action onComplete) {
        nextMinorMessagesQueue.Enqueue((title, message, onComplete));

        NextStepMinorMessage();
    }

    private void NextStepMinorMessage(bool forceNextItem = false) {
        if (nextMinorMessagesQueue.Count == 0) {
            minorPanel.gameObject.SetActive(false);
            playerBehaviour.SetInternalPause(false);
            return;
        }

        // If we were not in menu before, or we are moving to the next item, display next item
        if (!minorPanel.gameObject.activeSelf || forceNextItem) {
            var nextItem = nextMinorMessagesQueue.Peek();
            minorPanel.SetTitleAndText(nextItem.Item1, nextItem.Item2);
        }

        // If we were not in menu before, open menu and pause
        if(!minorPanel.gameObject.activeSelf) {
            minorPanel.gameObject.SetActive(true);
            playerBehaviour.SetInternalPause(true);
        }
    }

    public void ButtonDidClick(GameButton button) {
        if (button == majorPanel.continueButton) {
            majorPanel.gameObject.SetActive(false);

            currentMajorMessageAction?.Invoke();
            playerBehaviour.SetInternalPause(false);
        } else if (button == minorPanel.continueButton) {
            var currentMessage = nextMinorMessagesQueue.Dequeue();
            currentMessage.Item3?.Invoke();

            NextStepMinorMessage(true);
        }
    }
}
