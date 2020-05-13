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
    public Action eventAction;

    public TutorialEvent(string title, string message, TutorialTrigger? completionTrigger = null, bool remainPaused = false) {
        this.title = title;
        this.message = message;

        this.completionTrigger = completionTrigger;
        this.remainPaused = remainPaused;

        this.eventAction = null;
        addDelay = 0;
    }
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
        repeatButton.gameObject.SetActive(true);
        repeatButton.SetEnabled(false);

        playerBehaviour.SetInternalPause(true);

        sceneQueue = new Queue<TutorialScene>();
        isolateUserAction = new IsolatedUserAction();

        // Mine
        TutorialEvent mine1 = new TutorialEvent("New Task", "To start, lets add a new Mining Task.", null, true);
        TutorialEvent mine2 = new TutorialEvent("New Task", "Check the surrounding terrain for Soft Rock and select Mine.", TutorialTrigger.SelectMine, true);
        mine2.eventAction = () => {
            isolateUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { mine1, mine2 }));

        // Post-Mine
        TutorialEvent postMine1 = new TutorialEvent("Success", "A new task has been created!", null, true);
        TutorialEvent postMine2 = new TutorialEvent("Success", "On the right, click on the 'Mine Bots' panel to see a list of units and backlog of tasks.", TutorialTrigger.TaskAndUnitDetails, true);
        postMine2.eventAction = () => {
            isolateUserAction.SetTaskAndCellAction(MasterGameTask.ActionType.Mine, TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier.Details);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { postMine1, postMine2 }));

        // Details
        TutorialEvent taskDetails1 = new TutorialEvent("Details", "Here you can see the details of this Unit Type.", null, true);
        TutorialEvent taskDetails2 = new TutorialEvent("Details", "Here we have one Miner and one task in the Backlog", null, true);
        TutorialEvent taskDetails3 = new TutorialEvent("Details", "Watch as we continue; the Miner will be assigned and perform the Mine Task.", TutorialTrigger.TerraformComplete);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { taskDetails1, taskDetails2, taskDetails3 }));

        // Mine again...
        TutorialEvent mineAgain1 = new TutorialEvent("Task Complete", "The Miner has finished his task!", null, true);
        TutorialEvent mineAgain2 = new TutorialEvent("Task Complete", "Let's mine out the next section of Loose Rock! Watch the 'Mine Bots' area for a summary of Unit and Task state changes.", TutorialTrigger.TerraformComplete);
        mineAgain2.eventAction = () => {
            uIManager.PopToRoot();
            isolateUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { mineAgain1, mineAgain2 }));

        // Light Tower
        TutorialEvent tower1 = new TutorialEvent("Fantastic", "Great work!", null, true);
        TutorialEvent tower2 = new TutorialEvent("Line of Sight", "As we mine, our Light Towers will light up new areas for us to explore.", null, true);
        TutorialEvent tower3 = new TutorialEvent("Line of Sight", "Light towers have a limited range, and cannot see around corners. Lets create a new one!", null);
        TutorialEvent tower4 = new TutorialEvent("Line of Sight", "Select the empty tile in the revealed area and click on Build Building -> Light Tower.", TutorialTrigger.BuildingComplete);
        tower4.eventAction = () => {
            isolateUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildBuilding, Building.Blueprint.Tower);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { tower1, tower2, tower3, tower4 }));

        // Post Tower Recap
        TutorialEvent postTower1 = new TutorialEvent("Recap", "Great! Let's recap.", null, true);
        TutorialEvent postTower2 = new TutorialEvent("Recap", "To build a Light Tower, Move and Build tasks were created. The Mover immediately began work on the Moving task.", null, true);
        TutorialEvent postTower3 = new TutorialEvent("Recap", "Once the building had all the resources it needed, the Build task was assigned to the Builder.", null, true);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { postTower1, postTower2, postTower3 }));
        
        // Build Mover
        TutorialEvent buildMover1 = new TutorialEvent("Adding Help", "Lets create some extra Move Bots to help us gather resources!", null, true);
        TutorialEvent buildMover2 = new TutorialEvent("Adding Help", "Select any tile and click Build Unit -> Mover", TutorialTrigger.UnitAdded);
        buildMover2.eventAction = () => {
            isolateUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildUnit, Unit.Blueprint.Mover);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { buildMover1, buildMover2 }));

        // Destroy current mover
        TutorialEvent endMover1 = new TutorialEvent("Oh No!", "It looks like our current Mover is running out of power!", null, true);
        endMover1.addDelay = 8;
        endMover1.eventAction = () => {
            Unit mover = unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move).First();
            mover.skipDurationUpdates = true;
            mover.SetRemainingDuration(20);

            playerBehaviour.PanCameraToUnit(mover);
        };

        TutorialEvent endMover2 = new TutorialEvent("Oh No!", "All units have a limited duration, and this guy has run out of time.", TutorialTrigger.UnitFinishedDestroySelf);
        endMover2.eventAction = () => {
            Unit mover = unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move).First();
            mover.Shutdown();
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { endMover1, endMover2 }));

        //Task Lock Intro
        TutorialEvent lockIntro1 = new TutorialEvent("Task Delegation", "Wait, are we stuck? Lucky for us, Move tasks can be delegated to Mine and Build Bots!", null, true);
        lockIntro1.addDelay = 5;

        TutorialEvent lockIntro2 = new TutorialEvent("Task Delegation", "Take a look at the 'Move Bots' panel; On the right side we can see the number of tasks in our backlog.", null, true);
        TutorialEvent lockIntro3 = new TutorialEvent("Task Delegation", "Try clicking on this panel now.", TutorialTrigger.TaskLockToggle, true);
        lockIntro3.eventAction = () => {
            isolateUserAction.SetTaskAndCellAction(MasterGameTask.ActionType.Move, TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier.LockToggle);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { lockIntro1, lockIntro2, lockIntro3 }));

        // Task Lock Actions
        TutorialEvent lockAction1 = new TutorialEvent("Task Delegation", "Great job! Now that the Task List Lock has been toggled off, Move tasks will now be assigned to all units.", TutorialTrigger.UnitCompleted);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { lockAction1 }));

        // Task Lock Actions
        TutorialEvent sensor1 = new TutorialEvent("Complete", "Nicely done. Our new Move is built to replace our lost one.", null, true);
        TutorialEvent sensor2 = new TutorialEvent("Complete", "That's enough info for now. Check the Help menu in game for more details, and thanks for playing!", null, true);
        //TutorialEvent sensor2 = new TutorialEvent("Where Next?", "Now we are faced with a choice, which direction should we mine in?", TutorialTrigger.UnitCompleted);
        //TutorialEvent sensor3 = new TutorialEvent("Where Next?", "Our end goal is to build ", TutorialTrigger.UnitCompleted);


        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { sensor1, sensor2 }));

        messageManager.SetMajorMessage("Welcome!", "", () => {
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
        Script.Get<Narrator>().DoEndGameTransition();
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

        e.eventAction?.Invoke();
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