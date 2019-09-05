using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Narrator : MonoBehaviour
{
    PathfindingGrid grid;
    MapGenerator mapGenerator;
    MapsManager mapsManager;

    Constants constants;

    public List<Unit> startingUnits;    

    LayoutCoordinate spawnCoordinate;

    Queue<Action> initActionChunks;


    void Start() {
        initActionChunks = new Queue<Action>();

        initActionChunks.Enqueue(() => {
            grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
            mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
            mapsManager = Script.Get<MapsManager>();
            constants = GetComponent<Constants>();

            spawnCoordinate = mapGenerator.GenerateWorld(constants.mapCountX, constants.mapCountY);
        });


        initActionChunks.Enqueue(() => {
            grid.gameObject.transform.position = mapsManager.transform.position;
            grid.createGrid();
            //grid.BlurPenaltyMap(4); // No blurr today!        
        });

        initActionChunks.Enqueue(() => {
            Script.Get<BuildingManager>().Initialize();
        });

        initActionChunks.Enqueue(() => {
            Script.Get<BuildingManager>().Initialize();
            PathGridCoordinate[][] coordinatesForSpawnCoordinate = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(spawnCoordinate);

            int i = 0;
            foreach(Unit unit in startingUnits) {

                int horizontalComponent = 1;
                if(i == 1) {
                    horizontalComponent = 0;
                }

                WorldPosition worldPos = new WorldPosition(MapCoordinate.FromGridCoordinate(coordinatesForSpawnCoordinate[horizontalComponent][i]));
                unit.transform.position = worldPos.vector3;
                i++;

                UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();

                if(unitBuilding != null) {
                    unitBuilding.ProceedToCompleteBuilding();
                } else {
                    unit.Initialize();
                }

            }
        });

        initActionChunks.Enqueue(() => {
            WorldPosition spawnWorldPosition = new WorldPosition(new MapCoordinate(spawnCoordinate));

            Building building = Instantiate(Building.Blueprint.Tower.resource) as Building;
            building.transform.position = spawnWorldPosition.vector3;

            //buildingManager.BuildAt(building, spawnCoordinate, new BlueprintCost(1, 1, 1));
            building.ProceedToCompleteBuilding();

            Camera.main.transform.position = spawnWorldPosition.vector3 + new Vector3(0, 250, -400);
        });

        initActionChunks.Enqueue(() => {
            Script.Get<MiniMap>().Initialize();
        });

        StartCoroutine(InitializeScene());

        //NotificationPanel notificationManager = Script.Get<NotificationPanel>();

        //TimeManager timeManager = Script.Get<TimeManager>();

        //System.Action<int, float> createNotificationBlock = (seconds, percent) => {
        //    NotificationItem notificationItem = new NotificationItem(seconds.ToString(), null);
        //    notificationManager.AddNotification(notificationItem);
        //};

        //timeManager.AddNewTimer(20, createNotificationBlock, null);
    }

    IEnumerator InitializeScene() {

        FadePanel fadePanel = Script.Get<FadePanel>();
        fadePanel.DisplayPercentBar(true);

        float percent = 0;
        fadePanel.SetPercent(percent);

        float incrementalPercent = 1f / (float) initActionChunks.Count;

        while(initActionChunks.Count > 0) {
            Action initAction = initActionChunks.Dequeue();
            initAction();

            fadePanel.SetPercent(percent += incrementalPercent);
            yield return null;
        }

        fadePanel.FadeOut(false, null);
    }
}
