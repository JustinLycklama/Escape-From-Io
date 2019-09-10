using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationShip : Building, CanSceneChangeDelegate {

    private bool canSceneChange = false;

    protected override void CompleteBuilding() {
        MessageWindow messageWindow = UIManager.Blueprint.MessageWindow.Instantiate() as MessageWindow;

        Action okay = () => {
            FadePanel panel = Tag.FadePanel.GetGameObject().GetComponent<FadePanel>();

            Action completed = () => {
                canSceneChange = true;
            };

            TimeManager timeManager = Script.Get<TimeManager>();

            panel.FadeOut(true, completed);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.GameFinish, null, null, this, timeManager.globalTimer);
        };

        messageWindow.SetTitleAndText("SUCCESS", "You've created a ship to return to earth!");
        messageWindow.SetSingleAction(okay, "Continue");

        messageWindow.Display();
    }

    protected override void UpdateCompletionPercent(float percent) {
        
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return canSceneChange;
    }
}
