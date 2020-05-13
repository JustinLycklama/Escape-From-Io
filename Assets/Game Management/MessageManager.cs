using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour, GameButtonDelegate, HotkeyDelegate
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

        majorPanel.buttonDelegate = this;
        minorPanel.buttonDelegate = this;

        playerBehaviour.AddHotKeyDelegate(KeyCode.Space, this);
    }

    private void OnDestroy() {
        playerBehaviour.RemoveHotKeyDelegate(this);
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

    public void CloseCurrentMinorMessage() {

        (string, string, Action)? currentMessage = null;

        if (nextMinorMessagesQueue.Count > 0) {
            currentMessage = nextMinorMessagesQueue.Dequeue();
        }

        NextStepMinorMessage(true);

        if (currentMessage.HasValue) {
            currentMessage.Value.Item3?.Invoke();
        }        
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
            playerBehaviour.SetInternalPause(false);

            currentMajorMessageAction?.Invoke();
        } else if (button == minorPanel.continueButton) {
            CloseCurrentMinorMessage();
        }
    }

    /*
     * HotkeyDelegate Interface 
     * */

    public void HotKeyPressed(KeyCode key) {
        ButtonDidClick(minorPanel.continueButton);
    }

    public static string ipsum = "Contrary to popular belief, Lorem Ipsum is not simply random text. It has roots in a piece of classical Latin literature from 45 BC, making it over 2000 years old. Richard McClintock, a Latin professor at Hampden-Sydney College in Virginia, looked up one of the more obscure Latin words, consectetur, from a Lorem Ipsum passage, and going through the cites of the word in classical literature, discovered the undoubtable source. Lorem Ipsum comes from sections 1.10.32 and 1.10.33 of \"de Finibus Bonorum et Malorum\" (The Extremes of Good and Evil) by Cicero, written in 45 BC. This book is a treatise on the theory of ethics, very popular during the Renaissance. The first line of Lorem Ipsum, \"Lorem ipsum dolor sit amet..\", comes from a line in section 1.10.32.\r\n\r\nThe standard chunk of Lorem Ipsum used since the 1500s is reproduced below for those interested. Sections 1.10.32 and 1.10.33 from \"de Finibus Bonorum et Malorum\" by Cicero are also reproduced in their exact original form, accompanied by English versions from the 1914 translation by H. Rackham.\r\n\r\nThere are many variations of passages of Lorem Ipsum available, but the majority have suffered alteration in some form, by injected humour, or randomised words which don't look even slightly believable. If you are going to use a passage of Lorem Ipsum, you need to be sure there isn't anything embarrassing hidden in the middle of text. All the Lorem Ipsum generators on the Internet tend to repeat predefined chunks as necessary, making this the first true generator on the Internet. It uses a dictionary of over 200 Latin words, combined with a handful of model sentence structures, to generate Lorem Ipsum which looks reasonable. The generated Lorem Ipsum is therefore always free from repetition, injected humour, or non-characteristic words etc.";
}
