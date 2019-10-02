using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Narrator : MonoBehaviour, CanSceneChangeDelegate, SceneChangeListener {

    public AudioSource audioSource;

    PathfindingGrid grid;
    MapGenerator mapGenerator;
    MapsManager mapsManager;
    Constants constants;
    PlayerBehaviour playerBehaviour;

    public List<Unit> startingUnits;
    Queue<Action> initActionChunks;

    int generationStepTwoCount = 0;
    int lastGenerationIteration = 0;

    //LayoutCoordinate spawnCoordinate;

    private bool canSceneChange = false;

    void Start() {
        initActionChunks = new Queue<Action>();
      
        initActionChunks.Enqueue(() => {
            grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
            mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
            mapsManager = Script.Get<MapsManager>();
            constants = GetComponent<Constants>();
            playerBehaviour = Script.Get<PlayerBehaviour>();

            playerBehaviour.SetPauseState(true);
        });

        initActionChunks.Enqueue(() => {
            mapGenerator.GenerateWorldStepOne(constants.mapCountX, constants.mapCountY);

            generationStepTwoCount = mapGenerator.GenerateStepTwoCount();
        });

        int chunkCount = 4;

        for(int chunk = 0; chunk < chunkCount; chunk++) {
            int localChunk = chunk;
            initActionChunks.Enqueue(() => {
                int actionsPerChunk = Mathf.RoundToInt(generationStepTwoCount / chunkCount);
                int stoppingPoint = lastGenerationIteration + actionsPerChunk;

                // on the last chunk, complete all actions regardless of estimation
                if (localChunk == chunkCount - 1) {
                    stoppingPoint = generationStepTwoCount;
                }

                for(int iteration = lastGenerationIteration; iteration < stoppingPoint; iteration++) {
                    mapGenerator.GenerateWorldStepTwo(iteration);
                }

                lastGenerationIteration = stoppingPoint;
            });
        }
       
        initActionChunks.Enqueue(() => {
            mapGenerator.GenerateWorldStepThree();
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
            PathGridCoordinate[][] coordinatesForSpawnCoordinate = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(mapGenerator.spawnCoordinate);

            List<PathGridCoordinate> unitSpawnPositions = new List<PathGridCoordinate>() {
                coordinatesForSpawnCoordinate[0][1],
                coordinatesForSpawnCoordinate[1][0],
                coordinatesForSpawnCoordinate[2][1],
                coordinatesForSpawnCoordinate[1][2]
            };

            int i = 0;
            foreach(Unit unit in startingUnits) {
                WorldPosition worldPos = new WorldPosition(MapCoordinate.FromGridCoordinate(unitSpawnPositions[i]));
                unit.transform.position = worldPos.vector3;
                i++;

                UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();

                if(unitBuilding != null) {
                    unitBuilding.ProceedToCompleteBuilding();
                } else {
                    unit.Initialize();
                }

                if (i == startingUnits.Count - 1) {
                    unit.remainingDuration -= 75;
                }
            }
        });

        initActionChunks.Enqueue(() => {
            WorldPosition spawnWorldPosition = new WorldPosition(new MapCoordinate(mapGenerator.spawnCoordinate));

            Building building = Instantiate(Building.Blueprint.Tower.resource) as Building;
            building.transform.position = spawnWorldPosition.vector3;
            
            building.ProceedToCompleteBuilding();
            Script.Get<BuildingManager>().AddBuildingAtLocation(building, mapGenerator.spawnCoordinate);

            Script.Get<PlayerBehaviour>().JumpCameraToPosition(spawnWorldPosition.vector3);
        });

        initActionChunks.Enqueue(() => {
            Script.Get<MiniMap>().Initialize();
            StartCoroutine(CheckForNoRobots());
        });

        StartCoroutine(InitializeScene());

        SceneManagement.sharedInstance.RegisterForSceneUpdates(this);

        //NotificationPanel notificationManager = Script.Get<NotificationPanel>();

        //TimeManager timeManager = Script.Get<TimeManager>();

        //System.Action<int, float> createNotificationBlock = (seconds, percent) => {
        //    NotificationItem notificationItem = new NotificationItem(seconds.ToString(), null);
        //    notificationManager.AddNotification(notificationItem);
        //};

        //timeManager.AddNewTimer(20, createNotificationBlock, null);
    }

    private void OnDestroy() {
        SceneManagement.sharedInstance.EndSceneUpdates(this);
    }

    private void EndGameFailure() {
        MessageWindow messageWindow = UIManager.Blueprint.MessageWindow.Instantiate() as MessageWindow;

        Action okay = () => {
            FadePanel panel = Tag.FadePanel.GetGameObject().GetComponent<FadePanel>();

            Action completed = () => {
                canSceneChange = true;
            };

            panel.FadeOut(true, completed);
            SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.GameFinish, null, null, this, null);
        };

        messageWindow.SetTitleAndText("GAME OVER", "No robots remain to fulfill your goals.\nYou remain trapped on Io...");
        messageWindow.SetSingleAction(okay, "Continue");

        messageWindow.Display();
    }

    IEnumerator InitializeScene() {

        FadePanel fadePanel = Script.Get<FadePanel>();
        fadePanel.DisplayPercentBar(true);

        float percent = 0;
        fadePanel.SetPercent(percent);

        float incrementalPercent = 1f / ((float) initActionChunks.Count + 1);

        yield return null;
        while(initActionChunks.Count > 0) {
            Action initAction = initActionChunks.Dequeue();
            initAction();

            fadePanel.SetPercent(percent += incrementalPercent);
            yield return null;
        }

        // Wait for colliders to be built
        yield return new WaitUntil(delegate {
            return mapsManager.AnyBoxColliderBeingBuilt() == false;
        });

        fadePanel.SetPercent(percent += incrementalPercent);     
        fadePanel.FadeOut(false, null);

        playerBehaviour.SetPauseState(false);

        StartCoroutine(StartMusic());
    }

    IEnumerator StartMusic() {
        yield return new WaitForSeconds(2.5f);

        audioSource.Play();
    }

    IEnumerator CheckForNoRobots() {
        UnitManager unitManager = Script.Get<UnitManager>();

        while(true) {

            if (unitManager.GetUnitsOfType(MasterGameTask.ActionType.Build).Length == 0 &&
                unitManager.GetUnitsOfType(MasterGameTask.ActionType.Mine).Length == 0 &&
                unitManager.GetUnitsOfType(MasterGameTask.ActionType.Move).Length == 0) {

                Script.Get<PlayerBehaviour>().SetPauseState(true);
                yield return new WaitForSeconds(2);

                EndGameFailure();
                yield break;
            }

            yield return new WaitForSeconds(1);
        }
    }

    /*
     * CanSceneChangeDelegate Interface
     * */

    public bool CanWeSwitchScene() {
        return canSceneChange;
    }

    /*
     * SceneChangeListener Interface
     * */

    public void WillSwitchScene() {
        audioSource.Stop();
    }
}
