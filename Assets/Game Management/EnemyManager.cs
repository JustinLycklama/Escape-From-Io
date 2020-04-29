using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour, TimeUpdateDelegate
{

    public float frequency { get; private set; }
    public float evolution { get; private set; }


    private TimeManager timeManager;
    private UnitManager unitManager;

    private const float evoPerSecond = 1.0f / (20.0f * 60.0f);

    private const int minTimeBeforeEnemy = 0;
    private const int decisionFrequency = 10;

    private const float minEnemyPerMinute = 0.5f;
    private const float maxEnemyPerMinute = 6;

    private const float minEnemyStrength = 1;
    private const float maxEnemyStrength = 10;

    private const float minEnemyHealth = 1;
    private const float maxEnemyHealth = 10;

    private float falloverRemainder = 0;

    private System.Random rnd;

    // Start is called before the first frame update
    void Start()
    {
        timeManager = Script.Get<TimeManager>();
        unitManager = Script.Get<UnitManager>();

        timeManager.RegisterForTimeUpdateNotifications(this);


        frequency = 0.1f;
        evolution = 0.1f;

        rnd = NoiseGenerator.random;

    }

    private void OnDestroy() {
        timeManager?.EndTimeUpdateNotifications(this);
    }

    private void DecideMonsterSpawnIntervals() {

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

    private void SpawnEnemy() {
        Unit[] units = unitManager.GetAllPlayerUnits();

        if (units.Length == 0) {
            return;
        }

        Unit anyUnit = units[0];
        var targetPosition = new WorldPosition(anyUnit.transform.position);

        GameObject golemObject = Blueprint.Golem.ConstructAt(targetPosition);
        Golem golem = golemObject.GetComponent<Golem>();

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

    /*
      * TimeUpdateDelegate Interface
      * */

    public void SecondUpdated() {
        frequency = Mathf.Clamp(unitManager.GetAllPlayerUnits().Length / 20.0f, 0.1f, 1);
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

        public static Blueprint Golem = new Blueprint("Golem", typeof(Golem), "MinerIcon", "Golem", "Native Enemy");

        public Blueprint(string fileName, Type type, string iconName, string label, string description) :
            base(folder + fileName, type, iconName, label, description, new BlueprintCost(new Dictionary<MineralType, int>() { })) { } 

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
