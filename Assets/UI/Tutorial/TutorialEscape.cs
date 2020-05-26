using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEscape : TutorialObject {
    public string welcomeTitle => "Escape From Io";

    public string welcomeMessage => "Finally, let's figure out how to get off this planet. The goal of the game is to build a shuttle as fast as possible, and you can track your progress towards that goal by the number of Azure Ore you have found." +
        "\n\nThe shuttle is built by first building a Shuttle Frame. On each of the four outside tiles of the frame, we build Shuttle Components. Finally, once all components are built, we can build the shuttle and escape!" +
        "\n\nIn addition, each component you build will upgrade your units or buildings, so make sure to build the components early to get the bonus.";

    public Queue<TutorialScene> GetTutorialSceneQueue() {

        Constants constants = Script.Get<Constants>();
        MapGenerator mapGenerator = Script.Get<MapGenerator>();
        MapsManager mapsManager = Script.Get<MapsManager>();
        TerrainManager terrainManager = Script.Get<TerrainManager>();

        Queue<TutorialScene> sceneQueue = new Queue<TutorialScene>();

        TutorialEvent hunt1 = new TutorialEvent("Begin the Hunt", "Lets start the search for Azure Ore!", null, true);
        TutorialEvent hunt2 = new TutorialEvent("Begin the Hunt", "Mine out this section of hard rock, and lets see where it leads", TutorialTrigger.TerraformComplete, false);
        hunt2.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { hunt1, hunt2 }));

        TutorialEvent sensor1 = new TutorialEvent("Which Way?", "The hunt for Azure is important, and luckily we do not have to search randomly to find it", null, true);
        sensor1.addDelay = 4;
        TutorialEvent sensor2 = new TutorialEvent("Which Way?", "As you progress, Azure Sensors will help you explore in the right direction to get what you need quickly", null, true);
        TutorialEvent sensor3 = new TutorialEvent("Which Way?", "Try building a Azure Sensor now, and watch to see which direction it points", TutorialTrigger.BuildingAdded, true);
        sensor3.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildBuilding, Building.Blueprint.SensorTower);
        };

        TutorialEvent sensor4 = new TutorialEvent("Which Way?", "To speed it up lets also unlock the Move Task Lock, so all of our units can help", TutorialTrigger.BuildingComplete, false);
        sensor4.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetTaskAndCellAction(MasterGameTask.ActionType.Move, TaskAndUnitCell.TaskAndUnitCellTutorialIdentifier.LockToggle);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { sensor1, sensor2, sensor3, sensor4 }));


        TutorialEvent light1 = new TutorialEvent("Light it up", "Great! It looks like the sensor is pointing downwards. Lets explore in that direction", null, false);
        light1.addDelay = 3;
        TutorialEvent light2 = new TutorialEvent("Light it up", "Let's start by mining out the Loose Rock to the south", TutorialTrigger.TerraformComplete, false);
        light2.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { light1, light2 }));


        TutorialEvent cont1 = new TutorialEvent("Light it up", "Now, build a light tower to explore the new area", TutorialTrigger.BuildingComplete, false);
        cont1.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildBuilding, Building.Blueprint.Tower);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { cont1 }));


        //TutorialEvent azure0 = new TutorialEvent("Keep Searching", "Keep mining until we find Azure", TutorialTrigger.TerraformComplete, false);
        //azure0.addDelay = 3;
        //azure0.eventAction = (IsolatedUserAction) => {
        //    IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        //};

        TutorialEvent azure1 = new TutorialEvent("Azure!", "It looks like we have found some Azure, let's get mining!", TutorialTrigger.TerraformComplete, false);
        azure1.addDelay = 4;
        azure1.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        };

        TutorialEvent autoComplete = new TutorialEvent("Azure!", "Theres lots to mine here, so lets skip this part. All remaining rock has been mined out for you.", null, false);
        autoComplete.addDelay = 1;
        autoComplete.eventAction = (IsolatedUserAction) => {

            foreach(MapContainer mapContainer in mapsManager.mapContainers) {
                TerrainType[,] mapTerrain = mapGenerator.GetTerrainForMap(mapContainer);

                for(int x = 0; x < constants.layoutMapWidth; x++) {
                    for(int y = 0; y < constants.layoutMapHeight; y++) {
                        TerrainType terrain = mapTerrain[x, y];

                        if (terrain.regionType == RegionType.Type.Mountain) {                        
                            TerrainType? targetTerrain = terrainManager.CanTerriformTo(terrain);

                            if(targetTerrain == null) {
                                // Do not attempt to terraform a target point into the same terrain type
                                continue;
                            }

                            LayoutCoordinate layoutCoordinate = new LayoutCoordinate(x, y, mapContainer);
                            RegionType regionType = terrainManager.regionTypeMap[targetTerrain.Value.regionType];
                            //float targetTerrainHeight = regionType.plateauAtBase ? regionType.noiseBase : regionType.noiseMax;

                            TerraformTarget terraformTarget = new TerraformTarget(layoutCoordinate, targetTerrain.Value);
                            terraformTarget.percentage = 1.0f;

                            mapContainer.map.TerraformHeightMap(terraformTarget);
                            mapContainer.map.UpdateTerrainAtLocation(layoutCoordinate, targetTerrain.Value);

                            mapContainer.map.PlaceMineralsAtLocation(layoutCoordinate);
                        }
                    }
                }
            }
        };

        //TutorialEvent azure2 = new TutorialEvent("Azure!", "One more to go!", TutorialTrigger.TerraformComplete, false);
        //azure2.eventAction = (IsolatedUserAction) => {
        //    IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.Mine);
        //};

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { azure1, autoComplete }));

        TutorialEvent frame1 = new TutorialEvent("Shuttle Frame", "Each Shuttle Component costs 2 Azure. Since we have enough for a component, lets start a Frame", null, true);
        frame1.addDelay = 1;

        //TutorialEvent shuttle2 = new TutorialEvent("Azure!", "Each Shuttle Component costs 2 Azure. Since we have enough for a component, lets start a Frame", TutorialTrigger.TerraformComplete, false);

        TutorialEvent frame2 = new TutorialEvent("Shuttle Frame", "A Shuttle Frame houses 4 Shuttle Components, and the Shuttle itself", null, true);
        TutorialEvent frame3 = new TutorialEvent("Shuttle Frame", "It must be built on a space where all 4 adjacent tiles are also buildable", null, true);
        TutorialEvent frame4 = new TutorialEvent("Shuttle Frame", "The space in the center of the Azure ores looks perfect! Select the center tile, click Ship Parts and Shuttle Frame", TutorialTrigger.BuildingComplete, false);
        frame4.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildShip, Building.Blueprint.StationShipFrame);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { frame1, frame2, frame3, frame4 }));

        TutorialEvent component1 = new TutorialEvent("Shuttle Components", "Lets get started on first component", null, true);
        TutorialEvent component2 = new TutorialEvent("Shuttle Components", "Each component has a unique bonus once built. Choosing the correct bonus will help you explore even faster", null, true);
        TutorialEvent component3 = new TutorialEvent("Shuttle Components", "Lets build the Reactor. Once the Reactor is finished, all units will have an increased duration!", null, true);
        TutorialEvent component4 = new TutorialEvent("Shuttle Components", "Select any of the adjacent tiles to the frame and build a Reactor", TutorialTrigger.BuildingComplete, false);
        component4.eventAction = (IsolatedUserAction) => {
            IsolatedUserAction.SetUserActionIdentifier(UserAction.UserActionTutorialIdentifier.BuildShip, Building.Blueprint.Reactor);
        };

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { component1, component2, component3, component4 }));


        TutorialEvent complete1 = new TutorialEvent("Congrats!", "You've built your first Component!", null, true);
        TutorialEvent complete2 = new TutorialEvent("Congrats!", "Once all four Components are complete, you can build the Shuttle and escape!", null, true);

        TutorialEvent complete3 = new TutorialEvent("Congrats!", "Great job, you've finished the tutorials. Thanks for playing!", null, true);

        sceneQueue.Enqueue(new TutorialScene(new TutorialEvent[] { complete1, complete2, complete3 }));

        return sceneQueue;
    }
}
