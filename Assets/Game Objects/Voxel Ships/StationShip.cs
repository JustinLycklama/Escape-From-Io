using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationShip : Building, CanSceneChangeDelegate {

    private bool canSceneChange = false;

    protected override string title => "Starship";
    protected override float constructionModifierSpeed => 0.1f;

    protected override void CompleteBuilding() {
        MessageWindow messageWindow = UIManager.Blueprint.MessageWindow.Instantiate() as MessageWindow;

        TimeManager timeManager = Script.Get<TimeManager>();
        float completionTime = timeManager.globalTimer;

        Action okay = () => {
            FadePanel panel = Tag.FadePanel.GetGameObject().GetComponent<FadePanel>();

            Action completed = () => {
                canSceneChange = true;
            };

            panel.FadeOut(true, completed);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.GameFinish, null, null, this, completionTime);
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
