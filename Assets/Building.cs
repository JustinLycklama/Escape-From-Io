﻿using System;
using UnityEngine;

public interface ActionableItem {
    float performAction(GameTask task, float rate, Unit unit);
    void AssociateTask(GameTask task);
    string description { get; }
}

//public class BuildingManager {

//    public static BuildingManager sharedInstance = new BuildingManager();

//    public class Blueprint {

//        public Blueprint(string fileName, string description, Type type) {
//            this.fileName = fileName;
//            this.description = description;
//            this.type = type;

//            this.resource = Resources.Load(fileName, type);
//        }

//        public string fileName { get; set; }
//        public string description { get; set; }
//        public Type type { get; set; }

//        public UnityEngine.Object resource;

//        public static Blueprint Tower = new Blueprint("Tower", "Light Tower", typeof(Tower));
//        public static Blueprint Refinery = new Blueprint("Refinery", "Refinery", typeof(Refinery));

//    }

//    public Blueprint[] AvailableBuildings() {
//        return new Blueprint[] { Blueprint.Tower, Blueprint.Tower };
//    }
//}


public class Building : MonoBehaviour, ActionableItem, Selectable {
    float percentComplete = 0;
    Color materialColor;
    Color baseColor;

    Renderer buildingRenderer;

    public Material material;

    StatusDelegate statusDelegate;

    private void Awake() {
        title = "Building #" + buildingCount;
        buildingCount++;
    }

    // Start is called before the first frame update
    void Start()
    {
        buildingRenderer = GetComponent<Renderer>();
        //buildingRenderer.material.shader = Shader.Find("Transparent/Diffuse");

        baseColor = buildingRenderer.material.color;
        materialColor = baseColor;      
    }

    // Update is called once per frame
    void Update() {
        materialColor.a = Mathf.Clamp(percentComplete, 0.10f, 1f);
        buildingRenderer.material.color = materialColor;
    }


    // MARK: Selectable Interface
    public void SetSelected(bool selected) {
        Color tintColor = selected ? Color.cyan : baseColor;
        materialColor = tintColor;
    }

    public void SetStatusDelegate(StatusDelegate statusDelegate) {
        this.statusDelegate = statusDelegate;
    }

    // Actionable Item

    public void AssociateTask(GameTask task) { }

    // Returns the percent to completion the action is
    public float performAction(GameTask task, float rate, Unit unit) {
        switch(task.action) {
            case GameTask.ActionType.Build:
                percentComplete += rate;

                if(percentComplete > 1) {
                    percentComplete = 1;
                }

                return percentComplete;                       
            case GameTask.ActionType.DropOff:

                GameResourceManager.sharedInstance.ConsumeInBuilding(unit, this);

                return 1;
            case GameTask.ActionType.Mine:
            case GameTask.ActionType.PickUp:
            default:
                throw new System.ArgumentException("Action is not handled", task.action.ToString());
        }
    }

    public static int buildingCount = 0;

    string title;
    public string description => title;

    // STATIC

    public class Blueprint : PrefabBlueprint {
        public static Blueprint Tower = new Blueprint("Tower", "Light Tower", typeof(Tower));
        public static Blueprint Refinery = new Blueprint("Refinery", "Refinery", typeof(Refinery));

        public Blueprint(string fileName, string description, Type type) : base(fileName, description, type) {}

        public UserAction ConstructionAction(WorldPosition worldPosition) {
            UserAction action = new UserAction {
                description = "Build " + description,
                performAction = () => {

                    worldPosition.y += 25 / 2f;

                    Building building = UnityEngine.Object.Instantiate(resource) as Building;
                    building.transform.position = worldPosition.vector3;

                    TaskQueueManager queue = Script.Get<TaskQueueManager>();

                    GameTask oreTask = new GameTask(GameResourceManager.GatherType.Ore, GameTask.ActionType.PickUp, null);
                    oreTask.SatisfiesStartRequirements = () => {
                        return GameResourceManager.sharedInstance.AnyOreAvailable();
                    };

                    GameTask dropTask = new GameTask(worldPosition, GameTask.ActionType.DropOff, building);

                    GameTask buildTask = new GameTask(worldPosition, GameTask.ActionType.Build, building);

                    MasterGameTask masterCollectTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Build Building " + building.description, new GameTask[] { oreTask, dropTask });
                    MasterGameTask masterBuildTask = new MasterGameTask(MasterGameTask.ActionType.Build, "Build Building " + building.description, new GameTask[] { buildTask }, masterCollectTask);

                    queue.QueueTask(masterCollectTask);
                    queue.QueueTask(masterBuildTask);
                }
            };

            return action;
        }

    }

    public static Blueprint[] Blueprints() {
        return new Blueprint[] { Blueprint.Tower, Blueprint.Refinery };
    }
}