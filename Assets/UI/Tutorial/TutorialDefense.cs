using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialDefense : TutorialObject {
    public string welcomeTitle => "Self Defense";

    public string welcomeMessage => "As time passes, you will begin to see golems spawn and attack your units! In addition to your units duration, each unit only has a certain amount of health before it is destroyed. \n\nIn this tutorial we will cover building defensive units and buildings.";

    public Queue<TutorialScene> GetTutorialSceneQueue() {
        EnemyManager enemyManager = Script.Get<EnemyManager>();
        TimeManager timeManager = Script.Get<TimeManager>();

        Queue<TutorialScene> sceneQueue = new Queue<TutorialScene>();

        // Mine
        TutorialEvent info1 = new TutorialEvent("Defense Info", "Before we begin building, take a look at the top right info bar", null, true);
        TutorialEvent info2 = new TutorialEvent("Defense Info", "Here we can see enemy spawn rate and defensive unit info", null, true);
        TutorialEvent info3 = new TutorialEvent("Defense Info", "As we build more units, the frequency of enemies will increase! ", null, true);


        TutorialEvent info4 = new TutorialEvent("Defense Info", "The frequency has increased and enemies will spawn soon! lets mine out some rock and build some defense", TutorialTrigger.TerraformComplete, false);
        info4.addDelay = 1;
        info4.eventAction = (isolationObject) => {
            enemyManager.SetFrequencyAndEvo(0.25f, 0.1f);
            timeManager.SimulateSecondsUpdated();

            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        TutorialEvent info5 = new TutorialEvent("Defense Info", "A Defender costs 3 silver ore, and we have only found 2. Lets keep digging until we have enough silver", TutorialTrigger.TerraformComplete, false);
        info5.addDelay = 4;
        info5.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { info1, info2, info3, info4, info5 }));

        TutorialEvent defender1 = new TutorialEvent("The Defender", "Using the resources we've found, lets build a defender", null, true);
        TutorialEvent defender2 = new TutorialEvent("The Defender", "Select an empty tile, click build unit and select the Defender", TutorialTrigger.UnitAdded, false);
        defender2.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildUnit, Unit.Blueprint.Defender);
        };

        TutorialEvent defender3 = new TutorialEvent("The Defender", "A golem is appearing! During the game, you will recieve a notification when enemies spawn", null, true);
        defender3.addDelay = 3;
        defender3.eventAction = (isolationObject) => {
            enemyManager.SpawnEnemy();
        };

        TutorialEvent defender4 = new TutorialEvent("The Defender", "We had better hurry and finish this unit before the Golem does too much damage!", null, true);
        TutorialEvent defender5 = new TutorialEvent("The Defender", "Unlock the Mover Task Lock to the right of Move Bots panel to speed this task up", TutorialTrigger.TaskLockToggle, true);
        defender5.eventAction = (isolationObject) => {
            isolationObject.SetTaskAndCellAction(MasterGameTask.ActionType.Move, TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier.LockToggle);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { defender1, defender2, defender3, defender4, defender5 }));

        TutorialEvent watch1 = new TutorialEvent("Watch", "Lets watch and see how the Defender takes care of the Golem, once it is built", TutorialTrigger.UnitFinishedDestroySelf, false);
        TutorialEvent watch2 = new TutorialEvent("Watch", "Great! Now that the golem is taken care of, lets continue exploring to our right using a Light Tower", TutorialTrigger.BuildingComplete, false);
        watch2.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildBuilding, Building.Blueprint.Tower);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { watch1, watch2 }));

        TutorialEvent gold1 = new TutorialEvent("New Area", "Look at all of this gold! This looks like an area we are going to spend some time in", null, true);
        gold1.addDelay = 5;
        TutorialEvent gold2 = new TutorialEvent("New Area", "To secure it for the future, lets build a Turret here. Turrets require gold, so lets first mine some Hard Rock", TutorialTrigger.TerraformComplete, false);
        gold2.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        TutorialEvent gold3 = new TutorialEvent("New Area", "Great! Now lets build a Turret in the center, to keep this area safe for good", TutorialTrigger.BuildingComplete, false);
        gold2.eventAction = (isolationObject) => {
            isolationObject.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildBuilding, Building.Blueprint.DefenseTower);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { gold1, gold2, gold3 }));

        TutorialEvent strong1 = new TutorialEvent("Evolution", "The Evolution meter at the top right represents enemies strength. As time passes, enemies will become stronger", null, true);
        TutorialEvent strong2 = new TutorialEvent("Evolution", "The Evolution level has increased, and an enemy is spawning! Lets see how our Turret handles it", TutorialTrigger.UnitFinishedDestroySelf, false);
        strong2.addDelay = 1;
        strong2.eventAction = (isolationObject) => {
            enemyManager.SetFrequencyAndEvo(0.25f, 0.8f);
            timeManager.SimulateSecondsUpdated();

            enemyManager.SpawnEnemy();           
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { strong1, strong2 }));


        TutorialEvent complete1 = new TutorialEvent("Complete!", "As you can see, the Turrets are much more powerful than the Defender Units", null, false);
        TutorialEvent complete2 = new TutorialEvent("Complete!", "The downside is, they can only protect a small area. As you explore, you may need a combination to stay safe!", null, false);
        TutorialEvent complete3 = new TutorialEvent("Complete!", "Thats all for now! Checkout the Help Menu in game for more info, and thanks for playing!", null, false);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { complete1, complete2, complete3 }));

        return sceneQueue;
    }
}
