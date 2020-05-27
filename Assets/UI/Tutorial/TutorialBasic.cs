using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialBasic : TutorialObject {
    public string welcomeTitle => "The Basics";

    public string welcomeMessage => "Escape from Io is a task management game. We can assign tasks to the terrain, and delegate which unit types can perform them. Manage units quickly and efficiently to find the resources you need to escape, before you are overrun by the planets inhabitants! \n\nIn this tutorial you'll learn the basics of commanding your units and exploring the map. In later tutorials we will cover finding important resources, defending your units from enemies, and escaping the planet.\n\n Have fun!";

    public Queue<TutorialScene> GetTutorialSceneQueue() {

        UIManager uIManager = Script.Get<UIManager>();
        UnitManager unitManager = Script.Get<UnitManager>();
        PlayerBehaviour playerBehaviour = Script.Get<PlayerBehaviour>();


        Queue<TutorialScene> sceneQueue = new Queue<TutorialScene>();

        // Mine
        TutorialEvent mine1 = new TutorialEvent("New Task", "To start, lets add a new Mining Task.", null, true);
        TutorialEvent mine2 = new TutorialEvent("New Task", "Check the surrounding terrain for Loose Rock and select Mine.", TutorialTrigger.SelectMine, true);
        mine2.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { mine1, mine2 }));

        // Post-Mine
        TutorialEvent postMine1 = new TutorialEvent("Success", "A new task has been created!", null, true);
        TutorialEvent postMine2 = new TutorialEvent("Success", "On the right, click on the 'Mine Bots' panel to see a list of units and backlog of Tasks.", TutorialTrigger.TaskAndUnitDetails, true);
        postMine2.eventAction = (isolationObject) => {
            isolationObject.SetTaskAndCellAction(MasterGameTask.ActionType.Mine, TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier.Details);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { postMine1, postMine2 }));

        // Details
        TutorialEvent taskDetails1 = new TutorialEvent("Details", "Here you can see the details of this Unit Type.", null, true);
        TutorialEvent taskDetails2 = new TutorialEvent("Details", "Here we have one Miner and one Task in the Backlog", null, true);
        TutorialEvent taskDetails3 = new TutorialEvent("Details", "Watch as we continue; the Miner will be assigned and perform the Mine Task.", TutorialTrigger.TerraformComplete);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { taskDetails1, taskDetails2, taskDetails3 }));

        // Mine again...
        TutorialEvent mineAgain1 = new TutorialEvent("Task Complete", "The Miner has finished his task!", null, true);
        TutorialEvent mineAgain2 = new TutorialEvent("Task Complete", "Let's mine out the next section of Loose Rock!", TutorialTrigger.SelectMine);
        mineAgain2.eventAction = (isolationObject) => {
            uIManager.PopToRoot();
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };
        TutorialEvent mineAgain3 = new TutorialEvent("Task Complete", "Great. Now, watch the 'Mine Bots' area for a summary of Unit and Task state changes.", TutorialTrigger.TerraformComplete);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { mineAgain1, mineAgain2, mineAgain3 }));

        // Light Tower
        TutorialEvent tower1 = new TutorialEvent("Fantastic", "Great work!", null, true);
        TutorialEvent tower2 = new TutorialEvent("Line of Sight", "As we mine, our Light Towers will light up new areas for us to explore.", null, true);
        TutorialEvent tower3 = new TutorialEvent("Line of Sight", "Light towers have a limited range, and cannot see around corners. Lets create a new one!", null);
        TutorialEvent tower4 = new TutorialEvent("Line of Sight", "Select the empty tile in the revealed area and click on Build Building -> Light Tower.", TutorialTrigger.BuildingComplete);
        tower4.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildBuilding, Building.Blueprint.Tower);
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
        buildMover2.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildUnit, Unit.Blueprint.Mover);

            // For next scene
            Unit mover = unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move).First();
            mover.skipDurationUpdates = true;
            mover.SetRemainingDuration(20);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { buildMover1, buildMover2 }));

        // Destroy current mover
        TutorialEvent endMover1 = new TutorialEvent("Oh No!", "It looks like our current Mover is running out of power!", null, true);
        endMover1.addDelay = 8;
        endMover1.eventAction = (isolationObject) => {
            Unit mover = unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move).First();

            playerBehaviour.PanCameraToUnit(mover);
        };

        TutorialEvent endMover2 = new TutorialEvent("Oh No!", "All units have a limited duration, and this guy has run out of time.", TutorialTrigger.UnitFinishedDestroySelf);
        endMover2.eventAction = (isolationObject) => {
            Unit mover = unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move).First();
            mover.Shutdown();
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { endMover1, endMover2 }));

        //Task Lock Intro
        TutorialEvent lockIntro1 = new TutorialEvent("Task Delegation", "Wait, are we stuck? Lucky for us, Move tasks can be delegated to Mine and Build Bots!", null, true);
        lockIntro1.addDelay = 4;

        TutorialEvent lockIntro2 = new TutorialEvent("Task Delegation", "Take a look at the 'Move Bots' panel; On the right side we can see the number of tasks in our backlog.", null, true);
        TutorialEvent lockIntro3 = new TutorialEvent("Task Delegation", "Try clicking on this panel now.", TutorialTrigger.TaskLockToggle, true);
        lockIntro3.eventAction = (isolationObject) => {
            isolationObject.SetTaskAndCellAction(MasterGameTask.ActionType.Move, TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier.LockToggle);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { lockIntro1, lockIntro2, lockIntro3 }));

        // Task Lock Actions
        TutorialEvent lockAction1 = new TutorialEvent("Task Delegation", "Great job! Now that the Task List Lock has been toggled off, Move tasks will now be assigned to all units.", TutorialTrigger.UnitCompleted);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { lockAction1 }));

        // Task Lock Actions
        TutorialEvent sensor1 = new TutorialEvent("Complete!", "Nicely done. Our new Move is built to replace our lost one.", null, true);
        TutorialEvent sensor2 = new TutorialEvent("Complete!", "That's enough info for now. Thanks for playing!", null, true);
        //TutorialEvent sensor2 = new TutorialEvent("Where Next?", "Now we are faced with a choice, which direction should we mine in?", TutorialTrigger.UnitCompleted);
        //TutorialEvent sensor3 = new TutorialEvent("Where Next?", "Our end goal is to build ", TutorialTrigger.UnitCompleted);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { sensor1, sensor2 }));

        return sceneQueue;
    }
}
