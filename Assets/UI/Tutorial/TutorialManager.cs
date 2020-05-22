using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public enum TutorialTrigger {
    SelectMine, TerraformComplete, TaskAndUnitDetails, TaskLockToggle, BuildingComplete, BuildingAdded, UnitCompleted, UnitAdded, UnitFinishedDestroySelf
}

//public interface TutorialTriggerListener {
//    void EventFired(TutorialTrigger e);
//}

public struct TutorialEvent {
    public TutorialTrigger? completionTrigger;

    public string title;
    public string message;

    public bool remainPaused;

    public int addDelay;
    public Action<IsolatedUserAction> eventAction;

    public TutorialEvent(string title, string message, TutorialTrigger? completionTrigger = null, bool remainPaused = false) {
        this.title = title;
        this.message = message;

        this.completionTrigger = completionTrigger;
        this.remainPaused = remainPaused;

        this.eventAction = null;
        addDelay = 0;
    }
}

public interface TutorialObject {
    string welcomeTitle { get; }
    string welcomeMessage { get; }

    Queue<TutorialScene> GetTutorialSceneQueue();
}

public struct TutorialScene {
    public Queue<TutorialEvent> eventQueue;

    public TutorialScene(TutorialEvent[] events) {
        eventQueue = new Queue<TutorialEvent>(events);
    }
}


public class IsolatedUserAction {
    public UserAction.UserActionTutorialIdentifier? userActionIdentifier;
    
    public MasterGameTask.ActionType actionType;
    public TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier? cellIdentifier;

    public PrefabBlueprint blueprint;

    public void SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier? userActionIdentifier, PrefabBlueprint b = null) {
        Clear();

        this.userActionIdentifier = userActionIdentifier;
        blueprint = b;
    }

    public void SetTaskAndCellAction(MasterGameTask.ActionType actionType, TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier identifier) {
        Clear();

        this.actionType = actionType;
        this.cellIdentifier = identifier;
    }

    public void Clear() {
        userActionIdentifier = null;
        cellIdentifier = null;
        blueprint = null;
    }
}


public class TutorialManager: GameButtonDelegate {

    public static TutorialManager sharedInstance = new TutorialManager();

    public static bool isTutorial = false;
    public static IsolatedUserAction isolateUserAction = null;

    //private Dictionary<TutorialTrigger, TutorialTriggerListener> eventListenerMap = new Dictionary<TutorialTrigger, TutorialTriggerListener>();

    private Queue<TutorialScene> sceneQueue;

    private PlayerBehaviour playerBehaviour;
    private MessageManager messageManager;
    private UIManager uIManager;
    private UnitManager unitManager;

    private GameButton repeatButton;

    public TutorialObject tutorialObject;

    //TutorialScene? previousScene;
    TutorialScene? currentScene;
    Queue<TutorialEvent> currentEventQueue;
    TutorialTrigger? sceneWaitForTrigger = null;

    public void Fire(TutorialTrigger e) {
        if (sceneWaitForTrigger == e) {
            sceneWaitForTrigger = null;

            messageManager.CloseCurrentMinorMessage();
            uIManager.StartCoroutine(IterateScene());
        }
    }

    public void KickOffTutorial() {
        messageManager = Script.Get<MessageManager>();
        playerBehaviour = Script.Get<PlayerBehaviour>();
        uIManager = Script.Get<UIManager>();
        unitManager = Script.Get<UnitManager>();

        repeatButton = uIManager.tutorialRepeatButton;
        repeatButton.buttonDelegate = this;

        playerBehaviour.SetInternalPause(true);

        sceneQueue = tutorialObject?.GetTutorialSceneQueue();
        isolateUserAction = new IsolatedUserAction();   

        messageManager.SetMajorMessage(tutorialObject.welcomeTitle, tutorialObject.welcomeMessage, () => {

            repeatButton.transform.parent.gameObject.SetActive(true);
            repeatButton.SetEnabled(false);

            ContinueSceneQueue();
        });
    }

    /*
     * TutorialEventListener Implementation
     * */

    //public void ListenForEvent(TutorialTriggerListener updateDelegate, TutorialTrigger tutorialEvent) {
    //    eventListenerMap[tutorialEvent] = updateDelegate;
    //}

    //public void RemoveListener(TutorialTriggerListener updateDelegate) {
    //    foreach(TutorialTrigger e in eventListenerMap.Keys) {
    //        if (eventListenerMap[e] == updateDelegate) {
    //            eventListenerMap.Remove(e);
    //            return;
    //        }
    //    }
    //}

    //private void NotifyEvent(TutorialTrigger e) {
    //    if (eventListenerMap.ContainsKey(e)) {
    //        eventListenerMap[e].EventFired(e);
    //   }
    //}

    /*
     * Tutorial Events
     * */

    private void CompleteTutorial() {
        Script.Get<Narrator>().DoEndGameTransition(false);
    }

    private void ContinueSceneQueue() {

        if(sceneQueue.Count == 0) {
            CompleteTutorial();
            return;
        }

        currentScene = sceneQueue.Dequeue();
        ResetCurrentEventQueue();
    }

    private void ResetCurrentEventQueue() {

        sceneWaitForTrigger = null;
        isolateUserAction.Clear();

        currentEventQueue = new Queue<TutorialEvent>(currentScene.Value.eventQueue);

        uIManager.StartCoroutine(IterateScene());
    }

    private IEnumerator IterateScene() {

        if(currentEventQueue.Count == 0) {
            ContinueSceneQueue();       
            yield break;
        }

        repeatButton.SetEnabled(false);

        TutorialEvent e = currentEventQueue.Dequeue();

        if (e.addDelay != 0) {
            yield return new WaitForSeconds(e.addDelay);
        }

        e.eventAction?.Invoke(isolateUserAction);
        sceneWaitForTrigger = e.completionTrigger;

        messageManager.EnqueueMessage(e.title, e.message, () => {
            repeatButton.SetEnabled(true);

            if(e.remainPaused) {
                playerBehaviour.SetInternalPause(true);
            }

            if (e.completionTrigger == null) {
                uIManager.StartCoroutine(IterateScene());
            }
        });        
    }

    /*
     * GameButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        ResetCurrentEventQueue();
    }

    //private void PerformActionsForEvent(TutorialScene e) {



    //    switch(e) {
    //        case TutorialTrigger.Welcome:
    //            messageManager.SetMajorMessage("Welcome!", "", () => {

    //                ContinueSceneQueue();
    //            });
    //            break;
    //        case TutorialTrigger.FirstMine:
    //            messageManager.EnqueueMessage("Mine that", "Click Mine", () => {



    //            });
    //            break;
    //    }

    //}
}