using System;
using System.Collections.Generic;
using UnityEngine;

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


public abstract class Building : ActionableItem, Selectable {
    float percentComplete = 0;

    Dictionary<GameTask, float> percentPerTask;

    Color materialColor;
    Color baseColor;

    Renderer buildingRenderer;

    public Material material;

    //StatusDelegate statusDelegate;

    PercentageBar percentageBar;
    public Transform statusLocation;

    public int cost;

    //protected static abstract int Cost { get; }

    //protected abstract int requiredOre {
    //    get;
    //}

    private void Awake() {
        title = "Building #" + buildingCount;
        buildingCount++;

        percentageBar = UIManager.Blueprint.PercentageBar.Instantiate() as PercentageBar;
        percentageBar.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>());

        percentageBar.SetFollower(statusLocation);
        percentageBar.SetRequired(cost, "Ore");

        percentPerTask = new Dictionary<GameTask, float>();
    }

    // Start is called before the first frame update
    void Start()
    {
        buildingRenderer = GetComponent<Renderer>();
        //buildingRenderer.material.shader = Shader.Find("Transparent/Diffuse");

        baseColor = buildingRenderer.material.color;
        materialColor = baseColor;

        //UpdateGUI();
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

    //public void SetStatusDelegate(StatusDelegate statusDelegate) {
    //    this.statusDelegate = statusDelegate;
    //}

    // Actionable Item


    //public override void AssociateTask(GameTask task) { }

    // Returns the percent to completion the action is
    public override float performAction(GameTask task, float rate, Unit unit) {



        switch(task.action) {
            case GameTask.ActionType.Build:
                percentComplete += rate;

                if(percentComplete > 1) {
                    percentComplete = 1;
                }

                return percentComplete;                       
            case GameTask.ActionType.DropOff:

                if (!percentPerTask.ContainsKey(task)) {
                    percentPerTask.Add(task, 0);
                }

                percentPerTask[task] += rate;

                if (percentPerTask[task] >= 1) {
                    percentPerTask.Remove(task);

                    if(GameResourceManager.sharedInstance.ConsumeInBuilding(unit, this)) {
                        percentageBar.IncrementRequired();                        
                    }

                    return 1;
                }

                return percentPerTask[task];
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

    public class Blueprint : ConstructionBlueprint {
        private static String folder = "Buildings/";

        public static Blueprint Tower = new Blueprint("Tower", typeof(Tower), "Light Tower", 3);
        public static Blueprint Refinery = new Blueprint("Refinery", typeof(Refinery), "Refinery", 3);

        public Blueprint(string fileName, Type type, string label, int cost) : base(folder+fileName, type, label, cost) {}

        public override void ConstructAt(LayoutCoordinate layoutCoordinate) {
            WorldPosition worldPosition = new WorldPosition(new MapCoordinate(layoutCoordinate));

            Building building = UnityEngine.Object.Instantiate(resource) as Building;
            building.cost = cost;

            worldPosition.y += 0.5f * building.transform.localScale.y;
            building.transform.position = worldPosition.vector3;

            TaskQueueManager queue = Script.Get<TaskQueueManager>();

            GameTask oreTask = new GameTask("Find Ore", GameResourceManager.GatherType.Ore, GameTask.ActionType.PickUp, null);
            oreTask.SatisfiesStartRequirements = () => {
                return GameResourceManager.sharedInstance.AnyOreAvailable();
            };

            GameTask dropTask = new GameTask("Deposit Ore", worldPosition, GameTask.ActionType.DropOff, building);

            GameTask buildTask = new GameTask("Construction", worldPosition, GameTask.ActionType.Build, building);

            MasterGameTask masterCollectTask = new MasterGameTask(MasterGameTask.ActionType.Move, "Build Building " + building.description, new GameTask[] { oreTask, dropTask });
            masterCollectTask.repeatCount = this.cost;

            MasterGameTask masterBuildTask = new MasterGameTask(MasterGameTask.ActionType.Build, "Build Building " + building.description, new GameTask[] { buildTask }, masterCollectTask);

            queue.QueueTask(masterCollectTask);
            queue.QueueTask(masterBuildTask);                
        }
    }

    public static Blueprint[] Blueprints() {
        return new Blueprint[] { Blueprint.Tower, Blueprint.Refinery };
    }

    public void RegisterForTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        throw new NotImplementedException();
    }

    public void EndTaskStatusNotifications(TaskStatusUpdateDelegate notificationDelegate) {
        throw new NotImplementedException();
    }

    public void RegisterForUserActionNotifications(UserActionUpdateDelegate notificationDelegate) {
        throw new NotImplementedException();
    }

    public void EndUserActionNotifications(UserActionUpdateDelegate notificationDelegate) {
        throw new NotImplementedException();
    }
}
