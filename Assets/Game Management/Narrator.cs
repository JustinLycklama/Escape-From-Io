using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Narrator : MonoBehaviour, CanSceneChangeDelegate, SceneChangeListener, GameButtonDelegate {

    PathfindingGrid grid;
    MapGenerator mapGenerator;
    MapsManager mapsManager;
    Constants constants;
    PlayerBehaviour playerBehaviour;
    NotificationPanel notificationManager;
    MessageManager messageManager;
    BuildingManager buildingManager;
    UnitManager unitManager;

    Queue<Action> initActionChunks;
    Queue<(float, Func<float>)> animationActionChunks;

    int generationStepTwoCount = 0;
    int lastGenerationIteration = 0;

    [SerializeField]
    private Unit minerPrefab = null;
    [SerializeField]
    private Unit moverPrefab = null;
    [SerializeField]
    private Unit builderPrefab = null;
    [SerializeField]
    private Unit defenderPrefab = null;
    [SerializeField]
    private Unit golemPrefab = null;

    private List<Unit> startingUnits;

    private bool canSceneChange = false;

    public bool gameInitialized { get; private set; } = false;

    Building startingBuilding;
    GameTask buildBuilding;

    [SerializeField]
    private CanvasGroup uiCanvas = null;

    [SerializeField]
    private GameOverPanel gameOverPanel = null;

    float? completionTime = null;

    Coroutine gameOverRoutine;

    void Start() {

        gameOverPanel.fadeSpeed = 0.75f;
        gameOverPanel.continueButton.buttonDelegate = this;

        SetupInitChunks();
        SetupAnimationChunks();

        StartCoroutine(InitializeScene());

        SceneManagement.sharedInstance.RegisterForSceneUpdates(this);
    }

    private void OnDestroy() {
        SceneManagement.sharedInstance.EndSceneUpdates(this);
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

    /*
     * Scene Init Chunks
     * */

    private void SetupInitChunks() {
        initActionChunks = new Queue<Action>();

        initActionChunks.Enqueue(() => {
            grid = Tag.AStar.GetGameObject().GetComponent<PathfindingGrid>();
            mapGenerator = Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>();
            mapsManager = Script.Get<MapsManager>();
            constants = GetComponent<Constants>();
            playerBehaviour = Script.Get<PlayerBehaviour>();
            notificationManager = Script.Get<NotificationPanel>();
            messageManager = Script.Get<MessageManager>();
            buildingManager = Script.Get<BuildingManager>();
            unitManager = Script.Get<UnitManager>();

            notificationManager.SetSupressNotifications(true);
            playerBehaviour.SetInternalPause(true);

            mapsManager.mapBoundaryObject.gameObject.SetActive(false);
            uiCanvas.alpha = 0;
        });

        initActionChunks.Enqueue(() => {
            if(TutorialManager.isTutorial) {
                Tag.MapGenerator.GetGameObject().GetComponent<PremadeNoiseGenerator>()?.SetupCustomMap();
            }

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
                if(localChunk == chunkCount - 1) {
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
            buildingManager.Initialize();

            WorldPosition spawnWorldPosition = new WorldPosition(new MapCoordinate(mapGenerator.spawnCoordinate));
            spawnWorldPosition.y = 15f; // Hacky offset for Unknown tile

            buildBuilding = new GameTask("", new WorldPosition(), GameTask.ActionType.Build, null);

            startingBuilding = Instantiate(Building.Blueprint.Tower.resource) as Building;
            startingBuilding.allowFullTransparent = true;

            startingBuilding.transform.SetParent(buildingManager.transform);
            startingBuilding.transform.position = spawnWorldPosition.vector3;

            buildingManager.AddBuildingAtLocation(startingBuilding, mapGenerator.spawnCoordinate);
        });

        initActionChunks.Enqueue(() => {
            if(TutorialManager.isTutorial) {
                startingUnits = new List<Unit> { Instantiate(minerPrefab), Instantiate(moverPrefab), Instantiate(builderPrefab) };
            } else {
                startingUnits = new List<Unit> { Instantiate(minerPrefab), Instantiate(minerPrefab), Instantiate(moverPrefab), Instantiate(builderPrefab) };
            }

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
                unit.transform.SetParent(unitManager.transform);

                WorldPosition worldPos = new WorldPosition(MapCoordinate.FromGridCoordinate(unitSpawnPositions[i]));
                worldPos.y = 15f; // Hacky offset for Unknown tile

                unit.buildableComponent.allowFullTransparent = true;

                unit.transform.position = worldPos.vector3;
                unit.gameObject.SetActive(false);

                i++;

                //if(i == startingUnits.Count - 1) {
                //    unit.remainingDuration -= 75;
                //}
            }
        });

        initActionChunks.Enqueue(() => {
            Script.Get<MiniMap>().Initialize();
        });
    }

    private void SetupAnimationChunks() {
        animationActionChunks = new Queue<(float, Func<float>)>();

        animationActionChunks.Enqueue((0.1f, () => {
            return startingBuilding.performAction(buildBuilding, 2.5f * Time.deltaTime, null);
        }
        ));

        animationActionChunks.Enqueue((0.0f, () => {
            foreach(Unit unit in startingUnits) {
                unit.gameObject.SetActive(true);
            }

            return 1;
        }
        ));

        animationActionChunks.Enqueue((0.35f, () => {
            float lowest = float.MaxValue;

            foreach(Unit unit in startingUnits) {

                float percent = unit.buildableComponent.performAction(buildBuilding, 2.5f * Time.deltaTime, null);

                if (percent < lowest) {
                    lowest = percent;
                }
            }

            return lowest;
        }
        ));

        animationActionChunks.Enqueue((0.15f, () => {
            uiCanvas.alpha += Time.deltaTime * 1.5f;
            return uiCanvas.alpha;
        }
        ));
    }

    IEnumerator InitializeScene() {

        FadePanel fadePanel = Script.Get<FadePanel>();

        /*
         * Init Action Chunks
         * */

        float percent = 0;
        fadePanel.SetPercent(percent);

        float incrementalPercent = 1f / ((float) initActionChunks.Count + 1);
        while(initActionChunks.Count > 0) {

            yield return null;

            Action initAction = initActionChunks.Dequeue();
            initAction();

            fadePanel.SetPercent(percent += incrementalPercent);
        }

        yield return null;

        fadePanel.SetPercent(percent += incrementalPercent);
        fadePanel.FadeOut(false, true, null);
        gameOverPanel.FadeOut(false, true, null);

        WorldPosition spawnWorldPosition = new WorldPosition(new MapCoordinate(mapGenerator.spawnCoordinate));
        playerBehaviour.PanCameraToPosition(spawnWorldPosition.vector3, 0, false);

        /*
         * Init Animation Chunks
         * */

        yield return new WaitForSeconds(1f);

        float animatePercent = 0;
        float animationBaseline = 0;
        float animationTopline = 0;

        while(animationActionChunks.Count > 0) {

            (float, Func<float>) animationChunk = animationActionChunks.Dequeue();

            animationTopline = animationBaseline + animationChunk.Item1;

            animatePercent = 0;
            while(animatePercent < 1) {
                animatePercent = animationChunk.Item2();

                playerBehaviour.PanCameraToPosition(spawnWorldPosition.vector3, Mathf.Lerp(animationBaseline, animationTopline, animatePercent), false);
                yield return null;
            }

            animationBaseline = animationTopline;

            //yield return null;
        }

        // Wait for colliders to be built
        yield return new WaitUntil(delegate {
            return mapsManager.AnyBoxColliderBeingBuilt() == false;
        });

        mapsManager.mapBoundaryObject.gameObject.SetActive(true);

        playerBehaviour.SetInternalPause(false);
        StartCoroutine(StartMusic());
        //gameOverRoutine = StartCoroutine(CheckForNoRobots());

        // Animate camera pan with fog fade
        animatePercent = 0;
        animationTopline = 1.0f;

        while(animatePercent < 1) {
            animatePercent += Time.deltaTime / (MapContainer.fogOfWarFadeOutDuration + 1);

            playerBehaviour.PanCameraToPosition(spawnWorldPosition.vector3, Mathf.Lerp(animationBaseline, animationTopline, animatePercent), false);
            yield return null;
        }

        if(TutorialManager.isTutorial) {
            TutorialManager.sharedInstance.KickOffTutorial();
        } else {
            notificationManager.SetSupressNotifications(false);
        }
    }

    IEnumerator StartMusic() {
        yield return new WaitForSeconds(2.5f);

        AudioManager audioManager = Script.Get<AudioManager>();

        audioManager.PlayAudio(AudioManager.Type.Background1);

        float audioPercent = 0;
        while(audioPercent < 1) {
            audioManager.backgroundSource.volume = audioPercent;
            audioPercent += Time.deltaTime * 0.2f;

            yield return null;
        }
    }

    IEnumerator CheckForNoRobots() {
        yield return new WaitForSeconds(5);

        UnitManager unitManager = Script.Get<UnitManager>();

        while(true) {

            if(unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Build).Length == 0 &&
                unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Mine).Length == 0 &&
                unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move).Length == 0) {

                Script.Get<PlayerBehaviour>().SetInternalPause(true);
                yield return new WaitForSeconds(2);

                 while(uiCanvas.alpha > 0) {
                    uiCanvas.alpha -= Time.deltaTime * 1.5f;
                    yield return null;
                }

                EndGameFailure();
                yield break;
            }

            yield return new WaitForSeconds(1);
        }
    }

    public void EndGameSuccess() {
        StopCoroutine(gameOverRoutine);
        completionTime = (float) Script.Get<TimeManager>().currentDiscreteTime.TotalSeconds;

        playerBehaviour.SetInternalPause(true);

        gameOverPanel.SetSuccess();
        gameOverPanel.FadeOut(true, false, null);
    }

    private void EndGameFailure() {
        playerBehaviour.SetInternalPause(true);

        gameOverPanel.FadeOut(true, false, null);
    }

    public void DoEndGameTransition(bool displayHighscores = true) {       
        FadePanel panel = Script.Get<FadePanel>();

        Action completed = () => {
            canSceneChange = true;
        };

        panel.FadeOut(true, false, completed);
        SceneManagement.sharedInstance.ChangeScene(displayHighscores ? SceneManagement.State.GameFinish : SceneManagement.State.Title, null, null, this, completionTime);
    }

    /*
     * GameButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if (button == gameOverPanel.continueButton) {
            DoEndGameTransition();

            button.SetEnabled(false);
        }
    }
}
