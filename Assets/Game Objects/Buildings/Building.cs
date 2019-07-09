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

    //Renderer buildingRenderer;

    //public Material material;

    //StatusDelegate statusDelegate;

    //PercentageBar percentageBar;


    CostPanelTooltip costPanel;
    public Transform statusLocation;

    private BlueprintCost cost;

    //protected static abstract int Cost { get; }

    //protected abstract int requiredOre {
    //    get;
    //}

    private void Awake() {
        title = "Building #" + buildingCount;
        buildingCount++;

        //percentageBar.SetRequired(cost, "Ore");

        percentPerTask = new Dictionary<GameTask, float>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //buildingRenderer = GetComponent<Renderer>();
        //buildingRenderer.material.shader = Shader.Find("Transparent/Diffuse");

        //baseColor = buildingRenderer.material.color;
        //materialColor = baseColor;

        //UpdateGUI();
    }

    // Update is called once per frame
    //void Update() {
    //materialColor.a = Mathf.Clamp(percentComplete, 0.10f, 1f);
    //buildingRenderer.material.color = materialColor;
    //}

    public void SetCost(BlueprintCost cost) {
        this.cost = cost;

        costPanel = UIManager.Blueprint.CostPanel.Instantiate() as CostPanelTooltip;
        costPanel.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>());
        costPanel.toFollow = statusLocation;

        costPanel.SetCost(cost);
        costPanel.SetTallyMode(true);
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

    protected abstract void UpdateCompletionPercent(float percent);

    protected abstract void CompleteBuilding();



    // Returns the percent to completion the action is
    public override float performAction(GameTask task, float rate, Unit unit) {

        switch(task.action) {
            case GameTask.ActionType.Build:
                float previousPercent = percentComplete;
                percentComplete += rate;

                if(percentComplete > 1) {
                    percentComplete = 1;
                    CompleteBuilding();

                    BuildingManager buildingManager = Script.Get<BuildingManager>();
                    buildingManager.CompleteBuilding(this);

                } else {
                    if(Mathf.FloorToInt(previousPercent * 100) < Mathf.FloorToInt(percentComplete * 100)) {
                        UpdateCompletionPercent(percentComplete);
                    }
                }
              
                return percentComplete;                       
            case GameTask.ActionType.DropOff:

                if (!percentPerTask.ContainsKey(task)) {
                    percentPerTask.Add(task, 0);
                }

                percentPerTask[task] += rate;

                if(percentPerTask[task] >= 1) {
                    percentPerTask.Remove(task);

                    MineralType mineralType = Script.Get<GameResourceManager>().ConsumeInBuilding(unit, this);
                    costPanel.TallyMineralType(mineralType);

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

        public static Blueprint Tower = new Blueprint("Tower", typeof(Tower), "Light Tower", new BlueprintCost(3, 2, 1));
        public static Blueprint Refinery = new Blueprint("Refinery", typeof(Refinery), "Refinery", new BlueprintCost(1, 1, 1));

        public Blueprint(string fileName, Type type, string label, BlueprintCost cost) : base(folder+fileName, type, label, cost) {}

        public override void ConstructAt(LayoutCoordinate layoutCoordinate) {
            BuildingManager buildingManager = Script.Get<BuildingManager>();

            Building building = UnityEngine.Object.Instantiate(resource) as Building;
            buildingManager.BuildAt(building, layoutCoordinate, cost);
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
