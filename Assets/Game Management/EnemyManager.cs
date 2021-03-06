﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour, TimeUpdateDelegate
{

    public static float frequency { get; private set; }
    public static float evolution { get; private set; }

    private TimeManager timeManager;
    private UnitManager unitManager;

    private const int minTimeBeforeEnemy = 3 * 60;
    private const int decisionFrequency = 10;

    private const float maxUnitToFrequencyRatio = 20.0f; // at 20 units, frequency will be 1

    private const float minEnemyPerMinute = 0.2f;
    private const float maxEnemyPerMinute = 4;

    // At 25 mins, evo will top out at 1.0f;
    private const float maxEvoTime = 60.0f * 25.0f;
    private const float evoPerSecond = 1.0f / maxEvoTime;

    public static float minEnemyAttack = 1;
    public static float maxEnemyAttack = 10;

    //private const float minEnemyHealth = 1;
    //private const float maxEnemyHealth = 10;

    private float falloverRemainder = 0;

    private System.Random rnd;

    // Start is called before the first frame update
    void Start()
    {
        timeManager = Script.Get<TimeManager>();
        unitManager = Script.Get<UnitManager>();

        timeManager.RegisterForTimeUpdateNotifications(this);

        frequency = 0f;
        evolution = 0f;

        rnd = NoiseGenerator.random;
    }

    private void OnDestroy() {
        timeManager?.EndTimeUpdateNotifications(this);
    }

    private void DecideMonsterSpawnIntervals() {

        if (TutorialManager.isTutorial) {
            return;
        }

        float perMinuteReductionFactor = 60.0f / decisionFrequency;

        float enemiesPerMinute = Mathf.Lerp(minEnemyPerMinute, maxEnemyPerMinute, frequency);
        float enemiesPerDecision = enemiesPerMinute / perMinuteReductionFactor;

        int concreteEnemysThisDecicion = Mathf.FloorToInt(enemiesPerDecision);
        float remainder = enemiesPerDecision % 1;
        falloverRemainder += remainder;

        // If the remainder has made it over one, create the monster now
        if(falloverRemainder > 1) {
            falloverRemainder -= 1;
            concreteEnemysThisDecicion += 1;
        } else {
            // If we roll under the remainder, use create the monster now. If not, roll it over for the next Decision
            float roll = rnd.Next(0, 100) / 100.0f;

            if (roll < falloverRemainder) {
                falloverRemainder -= 1;
                concreteEnemysThisDecicion += 1;
            }
        }

        //print("concreteEnemysThisDecicion: " + concreteEnemysThisDecicion);
        //print("remainder: " + remainder);
        //print("falloverRemainder: " + falloverRemainder);

        for(int i = 0; i < concreteEnemysThisDecicion; i++) {
            float t = rnd.Next(0, 100) / 100.0f;
            int spawnAtTime = Mathf.RoundToInt(Mathf.Lerp(0, decisionFrequency, t));

            timeManager.AddNewTimer(spawnAtTime, null, () => {
                SpawnEnemy();
            });
        }
    }

    public void SpawnEnemy() {
        Unit[] units = unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Mine, Unit.FactionType.Player)
            .Concat(unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Move, Unit.FactionType.Player))
            .Concat(unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Attack, Unit.FactionType.Player))
            .ToArray();

        if (units.Length == 0) {
            return;
        }

        int i = NoiseGenerator.random.Next(units.Length);
        Unit anyUnit = units[i];       

        var targetPosition = new WorldPosition(anyUnit.transform.position);

        GameObject golemObject = Blueprint.Golem.ConstructAt(targetPosition);
        Golem golem = golemObject.GetComponent<Golem>();

        golem.transform.SetParent(unitManager.transform, true);
        golem.SetEvolution(evolution);

        GameTask buildingTask = new GameTask("", targetPosition, GameTask.ActionType.Build, golem.buildableComponent);

        int golemInitDuration = 4;
        float golemUpdateRate = (1.0f / (golemInitDuration * 10)) / golem.buildableComponent.constructionModifierSpeed;

        // Build ~95% of the unit
        timeManager.AddNewTimer(golemInitDuration,
            (time, percent) => {
                    // Terrain should not affect golem build speed
                    golem.buildableComponent.layoutTerrainModifier = 1;
                    golem.buildableComponent.performAction(buildingTask, golemUpdateRate, null);
            },
            () => {

                //Animate golem activating, wait for animation, then finish off the building action to initialize unit
                golem.ActiveAnimate();
                
                timeManager.AddNewTimer(6,
                    null,
                    () => {
                        golem.buildableComponent.performAction(buildingTask, 10, null);
                    });
            },
            1);
    }

    private IEnumerator GolemSpawnAnimateInit(Golem golem) {

        yield return new WaitForSeconds(4.0f);

        golem.ActiveAnimate();

        yield return new WaitForSeconds(4.0f);

        golem.Initialize();
    }

    public void SetFrequencyAndEvo(float freq, float evo) {
        frequency = freq;
        evolution = evo;
    }

    /*
      * TimeUpdateDelegate Interface
      * */

    public void SecondUpdated() {

        if(TutorialManager.isTutorial) {
            return;
        }

        float frequencyUnitRatio = Mathf.Clamp01(unitManager.GetAllPlayerUnits().Length / maxUnitToFrequencyRatio);
        float currentGameTimePercentage = Mathf.InverseLerp(minTimeBeforeEnemy, maxEvoTime, (float) timeManager.currentDiscreteTime.TotalSeconds);

        frequency = Mathf.Lerp(0, frequencyUnitRatio, currentGameTimePercentage);
        evolution = Mathf.Clamp01(evolution + evoPerSecond);

        if (timeManager.currentDiscreteTime.TotalSeconds > minTimeBeforeEnemy &&
            timeManager.currentDiscreteTime.TotalSeconds % decisionFrequency == 0) {
            DecideMonsterSpawnIntervals();
        }
    }

    /*
     * Blueprints
     * */

    public class Blueprint : ConstructionBlueprint {
        private static string folder = "Units/";

        public static Blueprint Golem = new Blueprint("Golem", typeof(Golem));

        public Blueprint(string fileName, Type type) :
            base(folder + fileName, type, null, null, null, null, new BlueprintCost(new Dictionary<MineralType, int>() { })) { } 

        public GameObject ConstructAt(WorldPosition worldPosition) {
            UnitManager unitManager = Script.Get<UnitManager>();
            Unit unit = UnityEngine.Object.Instantiate(resource) as Unit;

            UnitBuilding unitBuilding = unit.GetComponent<UnitBuilding>();
            unitBuilding.transform.position = worldPosition.vector3;

            return unit.gameObject;
        }

        public override GameObject ConstructAt(LayoutCoordinate layoutCoordinate) {
            throw new NotImplementedException();
        }
    }
}
