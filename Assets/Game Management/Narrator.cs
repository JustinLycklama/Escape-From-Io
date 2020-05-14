using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Narrator : MonoBehaviour, CanSceneChangeDelegate, SceneChangeListener {

    PathfindingGrid grid;
    MapGenerator mapGenerator;
    MapsManager mapsManager;
    Constants constants;
    PlayerBehaviour playerBehaviour;
    NotificationPanel notificationManager;
    MessageManager messageManager;

    public List<Unit> startingUnits;
    Queue<Action> initActionChunks;

    int generationStepTwoCount = 0;
    int lastGenerationIteration = 0;

    //LayoutCoordinate spawnCoordinate;

    private bool canSceneChange = false;

    public bool gameInitialized { get; private set; } = false; 

    void Start() {
        initActionChunks = new Queue<Action>();
      
        initActionChunks.Enqueue(() => {
            grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
            mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
            mapsManager = Script.Get<MapsManager>();
            constants = GetComponent<Constants>();
            playerBehaviour = Script.Get<PlayerBehaviour>();
            notificationManager = Script.Get<NotificationPanel>();
            messageManager = Script.Get<MessageManager>();

            notificationManager.SetSupressNotifications(true);

            if(TutorialManager.isTutorial) {
                Tag.MapGenerator.GetGameObject().GetComponent<PremadeNoiseGenerator>()?.SetupCustomMap();

                // Tutorial only start with 3 units
                for(int i = 3; i < startingUnits.Count; i++) {
                    Destroy(startingUnits[i]);
                }

                startingUnits.RemoveRange(3, startingUnits.Count - 3);
            }

            playerBehaviour.SetInternalPause(true);
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
                coordinatesForSpawnCoordinate[1][2],
                coordinatesForSpawnCoordinate[0][2],
                coordinatesForSpawnCoordinate[2][0],
                coordinatesForSpawnCoordinate[0][0],
                coordinatesForSpawnCoordinate[2][2]
            };

            int i = 0;
            foreach(Unit unit in startingUnits) {
                WorldPosition worldPos = new WorldPosition(MapCoordinate.FromGridCoordinate(unitSpawnPositions[i]));
                unit.transform.position = worldPos.vector3;
                i++;

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

            Script.Get<PlayerBehaviour>().InitCameraPosition(spawnWorldPosition.vector3);
        });

        initActionChunks.Enqueue(() => {
            Script.Get<MiniMap>().Initialize();
        });

        StartCoroutine(InitializeScene());

        SceneManagement.sharedInstance.RegisterForSceneUpdates(this);

        
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

    public void DoEndGameTransition() {
        FadePanel panel = Tag.FadePanel.GetGameObject().GetComponent<FadePanel>();

        Action completed = () => {
            canSceneChange = true;
        };

        panel.FadeOut(true, completed);
        SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.GameFinish, null, null, this, null);
    }

    private void EndGameFailure() {
        Action okay = () => {
            DoEndGameTransition();
        };

        Script.Get<MessageManager>().EnqueueMessage("GAME OVER", "No robots remain to fulfill your goals.\nYou remain trapped on Io...", okay);
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


        //Script.Get<MessageManager>().EnqueueMessage("Test", "This is an opening message!", null);
        //Script.Get<MessageManager>().EnqueueMessage("", "Second status message incoming", null);

        //messageManager.SetMajorMessage("Hello!", MessageManager.ipsum, null);

        // Init Units with delay
        foreach(Unit unit in startingUnits) {

            UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();

            if(unitBuilding != null) {
                unitBuilding.ProceedToCompleteBuilding();
            } else {
                unit.Initialize();
            }

            //yield return new WaitForSeconds(0.75f);
        }

        playerBehaviour.SetInternalPause(false);
        StartCoroutine(StartMusic());
        StartCoroutine(CheckForNoRobots());

        if(!TutorialManager.isTutorial) {
            notificationManager.SetSupressNotifications(false);
        } else {
            yield return new WaitForSeconds(MapContainer.fogOfWarFadeOutDuration + 0.5f);
            TutorialManager.sharedInstance.KickOffTutorial();
        }
    }

    IEnumerator StartMusic() {
        yield return new WaitForSeconds(2.5f);

        Script.Get<AudioManager>().PlayAudio(AudioManager.Type.Background1);       
    }

    IEnumerator CheckForNoRobots() {
        UnitManager unitManager = Script.Get<UnitManager>();

        while(true) {

            if (unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Build).Length == 0 &&
                unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Mine).Length == 0 &&
                unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move).Length == 0) {

                Script.Get<PlayerBehaviour>().SetInternalPause(true);
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
        //audioSource.Stop();
    }
}
