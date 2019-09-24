using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationShip : Building, CanSceneChangeDelegate, BuildingsUpdateDelegate {

    private bool canSceneChange = false;

    public override string title => "Starship Frame";
    protected override float constructionModifierSpeed => 0.15f;

    private LayoutCoordinate layoutCoordinate;

    void Start() {
        Constants constants = Script.Get<Constants>();

        WorldPosition worldPosition = new WorldPosition(transform.position);
        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

        layoutCoordinate = new LayoutCoordinate(mapCoordinate);
    }

    private void OnDestroy() {
        Script.Get<BuildingManager>()?.EndBuildingNotifications(this);
    }

    protected override void CompleteBuilding() {
        Script.Get<BuildingManager>().RegisterFoBuildingNotifications(this);
    }

    protected override void UpdateCompletionPercent(float percent) {
        
    }

    public void CheckComponentCompletion() {
        BuildingManager buildingManager = Script.Get<BuildingManager>();

        if (
            buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipThrusters)) &&
            buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipTelemetry)) &&
            buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipMachining)) &&
            buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipReactor)) 
            ) {
            CompleteGame();
        }
    }

    private void CompleteGame() {
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

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return canSceneChange;
    }

    /*
     * BuildingsUpdateDelegate Interface
     * */

    public void NewBuildingStarted(Building building) {
    }

    public void BuildingFinished(Building building) {
        CheckComponentCompletion();
    }
}
