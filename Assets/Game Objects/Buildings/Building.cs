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

    [Serializable]
    public class MeshBuildingTier {
        public MeshRenderer[] meshRenderes;

        [Range(0, 1)]
        public float aproximateTopPercentage;
    }

    public MeshBuildingTier[] meshTiers;

    public Transform statusLocation;

    private float percentComplete = 0;
    private Dictionary<GameTask, float> percentPerTask;

    Color materialColor;
    Color baseColor;

    private BlueprintCost cost;
    private CostPanelTooltip costPanel;   

    private Shader transparencyShader;
    private Shader tintableShader;

    //private Dictionary<MeshRenderer, Shader> originalShaderMap;

    public List<UserActionUpdateDelegate> userActionDelegateList = new List<UserActionUpdateDelegate>();

    private void Awake() {
        title = "Building #" + buildingCount;
        buildingCount++;

        percentPerTask = new Dictionary<GameTask, float>();
    }

    // Start is called before the first frame update
    void Start() {
        // Set transparent shader for all objects in the MeshRenderTier
        transparencyShader = Shader.Find("Custom/Buildable");
        tintableShader = Shader.Find("Custom/Tintable");

        //originalShaderMap = new Dictionary<MeshRenderer, Shader>();        

        foreach(MeshBuildingTier tier in meshTiers) {
            foreach(MeshRenderer meshRenderer in tier.meshRenderes) {
                //originalShaderMap[meshRenderer] = meshRenderer.material.shader;
                meshRenderer.material.shader = transparencyShader;
            }           
        }
    }

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
        Color color = Color.white;
        if(selected) {
            color = PlayerBehaviour.tintColor;
        }

        foreach(Building.MeshBuildingTier meshTier in meshTiers) {
            foreach(MeshRenderer renderer in meshTier.meshRenderes) {
                renderer.material.SetColor("_Color", color);
            }
        }
    }

    //public void SetStatusDelegate(StatusDelegate statusDelegate) {
    //    this.statusDelegate = statusDelegate;
    //}

    // Actionable Item


    //public override void AssociateTask(GameTask task) { }

    private void ProceedToUpdateCompletePercent(float percent) {

        // Update our Mesh Renderer Tiers
        float tierBase = 0f;
        
        foreach(MeshBuildingTier tier in meshTiers) {
            if (percent > tierBase && percent <= tier.aproximateTopPercentage) {
                foreach(MeshRenderer meshRenderer in tier.meshRenderes) {
                    meshRenderer.material.SetFloat("percentComplete", Mathf.InverseLerp(tierBase, tier.aproximateTopPercentage, percent));
                }                
            }

            tierBase = tier.aproximateTopPercentage;
        }
  
        UpdateCompletionPercent(percent);
    }

    protected abstract void UpdateCompletionPercent(float percent);

    public void ProceedToCompleteBuilding() {

        // Reset each renderer to its original shader
        foreach(MeshBuildingTier tier in meshTiers) {
            foreach(MeshRenderer meshRenderer in tier.meshRenderes) {
                meshRenderer.material.SetFloat("percentComplete", 1);
                meshRenderer.material.shader = tintableShader;
            }
        }

        if (costPanel != null && costPanel.isActiveAndEnabled) {
            costPanel.gameObject.SetActive(false);
            costPanel.transform.SetParent(null);
        }        

        CompleteBuilding();

        BuildingManager buildingManager = Script.Get<BuildingManager>();
        buildingManager.CompleteBuilding(this);
    }

    protected abstract void CompleteBuilding();

    // Returns the percent to completion the action is
    public override float performAction(GameTask task, float rate, Unit unit) {

        switch(task.action) {
            case GameTask.ActionType.Build:
                float previousPercent = percentComplete;
                percentComplete += rate;

                if(percentComplete > 1) {
                    percentComplete = 1;

                    ProceedToCompleteBuilding();
                } else {
                    if(Mathf.FloorToInt(previousPercent * 100) < Mathf.FloorToInt(percentComplete * 100)) {
                        ProceedToUpdateCompletePercent(percentComplete);
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
        private static string folder = "Buildings/";

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

    /*
     * UserActionNotifiable Interface
     * */

    public void RegisterForUserActionNotifications(UserActionUpdateDelegate notificationDelegate) {
        userActionDelegateList.Add(notificationDelegate);

        // Let the subscriber know our status immediately
        notificationDelegate.UpdateUserActionsAvailable(null);
    }

    public void EndUserActionNotifications(UserActionUpdateDelegate notificationDelegate) {
        userActionDelegateList.Remove(notificationDelegate);
    }

    public void NotifyAllUserActionsUpdate() {
        foreach(UserActionUpdateDelegate updateDelegate in userActionDelegateList) {
            updateDelegate.UpdateUserActionsAvailable(null);
        }
    }
}
