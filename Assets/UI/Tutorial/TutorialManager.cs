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

public class TutorialEvent {
    public TutorialTrigger? completionTrigger;

    public string title;
    public string message;

    public bool remainPaused;

    public int addDelay;
    public Action<IsolatedUserAction> eventAction;
    public bool postTeraformWait = false;

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

public enum TutorialType {
    Basic, Defense, Escape
}


public class TutorialManager: GameButtonDelegate, SceneChangeListener {

    public static TutorialManager sharedInstance = new TutorialManager();

    public static bool isTutorial {
        get {
            return sharedInstance.tutorialType != null;
        }
    }
    public static IsolatedUserAction isolateUserAction = null;

    public static float tutorialActionModifierSpeed = 1.0f;

    public TutorialType? tutorialType = null;

    //private Dictionary<TutorialTrigger, TutorialTriggerListener> eventListenerMap = new Dictionary<TutorialTrigger, TutorialTriggerListener>();

    private Queue<TutorialScene> sceneQueue;

    private PlayerBehaviour playerBehaviour;
    private MessageManager messageManager;
    private UIManager uIManager;
    private UnitManager unitManager;

    private GameButton repeatButton;

    private TutorialObject tutorialObject;

    //TutorialScene? previousScene;
    TutorialScene? currentScene;
    Queue<TutorialEvent> currentEventQueue;
    TutorialTrigger? sceneWaitForTrigger = null;
    bool waitForPostTeraform = false;

    public void Fire(TutorialTrigger e) {
        if (sceneWaitForTrigger == e) {
            if (waitForPostTeraform == true) {

                waitForPostTeraform = false;
                sceneWaitForTrigger = TutorialTrigger.TerraformComplete;

                return;
            }

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

        SceneManagement.sharedInstance.RegisterForSceneUpdates(this);

        Script.Get<EnemyManager>().SetFrequencyAndEvo(0, 0.1f);
        Script.Get<TimeManager>().SimulateSecondsUpdated();

        Script.Get<NotificationPanel>().gameObject.SetActive(false);

        repeatButton = uIManager.tutorialRepeatButton;
        repeatButton.buttonDelegate = this;

        if (tutorialType == null) {
            return;
        }

        switch(tutorialType.Value) {
            case TutorialType.Basic:
                tutorialObject = new TutorialBasic();
                break;
            case TutorialType.Defense:
                tutorialObject = new TutorialDefense();
                break;
            case TutorialType.Escape:
                tutorialObject = new TutorialEscape();
                break;
        }

        sceneQueue = tutorialObject?.GetTutorialSceneQueue();
        isolateUserAction = new IsolatedUserAction();   

        messageManager.SetMajorMessage(tutorialObject.welcomeTitle, tutorialObject.welcomeMessage, () => {

            repeatButton.transform.parent.gameObject.SetActive(true);
            repeatButton.SetEnabled(false);

            ContinueSceneQueue();
        });
    }

    /*
     * Tutorial Events
     * */

    private void CompleteTutorial() {
        isolateUserAction = null;
        tutorialType = null;

        Script.Get<Narrator>().DoEndGameTransition(false);
    }

    private void ContinueSceneQueue() {

        if(sceneQueue.Count == 0) {
            CompleteTutorial();
            return;
        }

        Action cont = () => {
            currentScene = sceneQueue.Dequeue();
            ResetCurrentEventQueue();
        };

        if (tutorialType != TutorialType.Basic) {
            uIManager.StartCoroutine(AddDelay(0.5f, cont));
        } else {
            cont();
        }        
    }

    private void ResetCurrentEventQueue() {

        sceneWaitForTrigger = null;
        isolateUserAction.Clear();

        currentEventQueue = new Queue<TutorialEvent>(currentScene.Value.eventQueue);

        uIManager.StartCoroutine(IterateScene());
    }

    private IEnumerator AddDelay(float delay, Action complete) {
        yield return new WaitForSeconds(delay);
        complete?.Invoke();
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
        waitForPostTeraform = e.postTeraformWait;

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
        foreach(TutorialEvent e in currentScene.Value.eventQueue) {
            e.addDelay = 0;
        }

        ResetCurrentEventQueue();
    }

    /*
     * SceneChangeListener Interface
     * */

    public void WillSwitchScene() {
        sceneWaitForTrigger = null;
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