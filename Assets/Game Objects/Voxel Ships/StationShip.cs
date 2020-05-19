using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationShip : Building, CanSceneChangeDelegate /*, BuildingsUpdateDelegate*/ {

    private bool canSceneChange = false;

    public override string title => "Starship Frame";
    public override float constructionModifierSpeed => 0.15f;

    private void OnDestroy() {
        //Script.Get<BuildingManager>()?.EndBuildingNotifications(this);
    }

    protected override void CompleteBuilding() {
        CompleteGame();


        //Script.Get<BuildingManager>().RegisterFoBuildingNotifications(this);
    }

    protected override void UpdateCompletionPercent(float percent) {
        
    }

    //public void CheckComponentCompletion() {
    //    BuildingManager buildingManager = Script.Get<BuildingManager>();

    //    if (
    //        buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipThrusters)) &&
    //        buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipTelemetry)) &&
    //        buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipMachining)) &&
    //        buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipReactor)) 
    //        ) {
    //        CompleteGame();
    //    }
    //}

    private void CompleteGame() {
        TimeManager timeManager = Script.Get<TimeManager>();
        float completionTime = timeManager.globalTimer;

        Action okay = () => {
            FadePanel panel = Tag.FadePanel.GetGameObject().GetComponent<FadePanel>();

            Action completed = () => {
                canSceneChange = true;
            };

            panel.FadeOut(true, false, completed);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.GameFinish, null, null, this, completionTime);
        };

        Script.Get<MessageManager>().EnqueueMessage("SUCCESS", "You've created a ship to return to earth!", okay);
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return canSceneChange;
    }

    /*
     * BuildingsUpdateDelegate Interface
     * */

    //public void NewBuildingStarted(Building building) {
    //}

    //public void BuildingFinished(Building building) {
    //    CheckComponentCompletion();
    //}
}
