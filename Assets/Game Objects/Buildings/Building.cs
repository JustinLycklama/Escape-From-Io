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
        public Renderer[] meshRenderes;

        [Range(0, 1)]
        public float aproximateTopPercentage;
    }

    public MeshBuildingTier[] meshTiers;

    public Transform statusLocation;

    public static int buildingCount = 0;

    public override string description => title;

    public abstract string title { get; }
    public abstract float constructionModifierSpeed { get; }

    public float layoutTerrainModifier;

    public bool buildingComplete = false;

    public LayoutCoordinate buildingLayoutCoordinate;

    // Building status
    private float percentComplete = 0;
    private Dictionary<GameTask, float> percentPerTask;

    private BlueprintCost cost;
    private CostPanelTooltip costPanel;   

    // Building Shaders
    private Shader transparencyShader;
    private Shader tintableShader;
    private Shader uniformTransparencyShader;

    public List<UserActionUpdateDelegate> userActionDelegateList = new List<UserActionUpdateDelegate>();

    protected virtual void Awake() {
        //title = "Building #" + buildingCount;
        buildingCount++;

        percentPerTask = new Dictionary<GameTask, float>();

        // Set transparent shader for all objects in the MeshRenderTier
        transparencyShader = Shader.Find("Custom/Buildable");
        tintableShader = Shader.Find("Custom/Tintable");
        uniformTransparencyShader = Shader.Find("Custom/BuildableUniform");

        SetTransparentShaders();

        gameObject.isStatic = true;
    }

    protected virtual void Start() {
        if(Script.Get<MapsManager>().initialized) {
            WorldPosition worldPosition = new WorldPosition(transform.position);
            MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);

            buildingLayoutCoordinate = new LayoutCoordinate(mapCoordinate);

            layoutTerrainModifier = Mathf.Clamp01(buildingLayoutCoordinate.mapContainer.map.GetTerrainAt(buildingLayoutCoordinate).modificationSpeedModifier * 2);
        } 
    }

    public void SetCost(BlueprintCost cost) {
        this.cost = cost;

        costPanel = UIManager.Blueprint.CostPanel.Instantiate() as CostPanelTooltip;
        costPanel.transform.SetParent(Script.UIOverlayPanel.GetFromObject<RectTransform>());
        costPanel.followPosition = statusLocation;
        costPanel.followingObject = transform;

        costPanel.SetCost(cost);
        costPanel.SetTitle(title);
        costPanel.SetTallyMode(true);
    }

    public override void Destroy() {
        deletionWatcher?.ObjectDeleted(this);

        if(costPanel != null) {

            // If we haven't completed our building, move the resources that have been placed here back onto the map
            if (buildingComplete == false) {
                costPanel.MoveUnusedCostToResourceManager();
            }

            costPanel.transform.SetParent(null);
            //costPanel.gameObject.SetActive(false);
            Destroy(costPanel.gameObject);
        }

        Script.Get<BuildingManager>().RemoveBuilding(this);

        transform.SetParent(null);
        Destroy(gameObject);
    }

    /*
     * Building Status Effects
     * */

    public virtual BuildingEffectStatus BuildingStatusEffects() {
        return BuildingEffectStatus.None;
    }

    public virtual int BuildingStatusRange() {
        return 0;
    }

    /*
     * Selectable Interface
     * */

    public void SetSelected(bool selected) {
        Color color = Color.white;
        if(selected) {
            color = ColorSingleton.sharedInstance.highlightColor;
        }

        foreach(Building.MeshBuildingTier meshTier in meshTiers) {
            foreach(Renderer renderer in meshTier.meshRenderes) {
                renderer.material.SetColor("_Color", color);
            }
        }
    }

    DeletionWatch deletionWatcher;
    public void SubscribeToDeletionWatch(DeletionWatch watcher) {
        deletionWatcher = watcher;
    }

    public void EndDeletionWatch(DeletionWatch watcher) {
        if(deletionWatcher == watcher) {
            deletionWatcher = null;
        }
    }

    /*
     * Shader and Alpha 
     * */

    public void SetTransparentShaders() {

        if (meshTiers != null && meshTiers.Length == 0) {
            return;
        }

        SetShaders(uniformTransparencyShader);

        //var uniform = (meshTiers[0].meshRenderes[0] is SkinnedMeshRenderer);

        //if (uniform) {
        //    SetShaders(uniformTransparencyShader);
        //} else {
        //    SetShaders(transparencyShader);
        //}        
    }

    public void SetTintableShaders() {
        SetAlphaPercentage(1);
        SetShaders(tintableShader);
    }

    private void SetShaders(Shader shader) {
        foreach(MeshBuildingTier tier in meshTiers) {
            foreach(Renderer meshRenderer in tier.meshRenderes) {
                meshRenderer.material.shader = shader;
            }
        }
    }

    public void SetAlphaPercentage(float percent) {
        
        // Update our Mesh Renderer Tiers
        float tierBase = 0f;

        foreach(MeshBuildingTier tier in meshTiers) {
            if(percent > tierBase && percent <= tier.aproximateTopPercentage) {
                float localPercent = Mathf.InverseLerp(tierBase, tier.aproximateTopPercentage, percent);

                foreach(Renderer meshRenderer in tier.meshRenderes) {                                        
                    meshRenderer.material.SetFloat("percentComplete", localPercent);
                }

                //print("base " + tierBase);
                //print("top " + tier.aproximateTopPercentage);
                //print("local% " + localPercent);
            }

            tierBase = tier.aproximateTopPercentage;
        }
    }

    public void SetAlphaSolid() {
        foreach(MeshBuildingTier tier in meshTiers) {           
            foreach(Renderer meshRenderer in tier.meshRenderes) {

                meshRenderer.material.SetFloat("percentComplete", 1);
            }            
        }
    }

    /*
    * Building Process Workflow
    * */

    private void ProceedToUpdateCompletePercent(float percent) {
        SetAlphaPercentage(percent);
        UpdateCompletionPercent(percent);
    }

    protected abstract void UpdateCompletionPercent(float percent);

    public void ProceedToCompleteBuilding() {

        SetTintableShaders();

        if (costPanel != null && costPanel.isActiveAndEnabled) {
            costPanel.gameObject.SetActive(false);
            costPanel.transform.SetParent(null);
        }

        buildingComplete = true;
        CompleteBuilding();

        BuildingManager buildingManager = Script.Get<BuildingManager>();
        buildingManager.CompleteBuilding(this);
    }

    protected abstract void CompleteBuilding();

    /*
     * ActionableItem Interface
     * */

    public override float performAction(GameTask task, float rate, Unit unit) {

        switch(task.action) {
            case GameTask.ActionType.Build:
                float previousPercent = percentComplete;
                percentComplete += rate * constructionModifierSpeed; // * layoutTerrainModifier;

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

    /*
     * Blueprints
     * */

    public class Blueprint : ConstructionBlueprint {
        private static string folder = "Buildings/";

        public static Blueprint PathBuilding = new Blueprint("PathBuilding", typeof(PathBuilding), "TowerIcon", "Path", "Units move faster along a path.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 1 }
            }));

        public static Blueprint TerraformLandBuilding = new Blueprint("PathBuilding", typeof(TerraformLandBuilding), "TowerIcon", "Terraform Land", "Create a path over water.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 1 }
            }));

        public static Blueprint Tower = new Blueprint("Tower", typeof(Tower), "TowerIcon", "Light Tower", "Provides vision in all directions until obstructed.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 }                
            }));

        public static Blueprint SensorTower = new Blueprint("SensorTower", typeof(SensorTower), "TowerIcon", "Azure Sensor", "Points to the closest Azure vein.",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 4 },
                { MineralType.Silver, 2 },
                { MineralType.Gold, 1 }
            }));

        public static Blueprint DefenseTower = new Blueprint("DefenderTower", typeof(DefenderTower), "TowerIcon", "Defender Turret", "Attacks Enemies",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                        //{ MineralType.Copper, 1 },
                        { MineralType.Silver, 4 },
                        { MineralType.Gold, 1 }
            }));

        public static Blueprint AdvUnitBuilding = new Blueprint("AdvUnitBuilding", typeof(AdvUnitBuilding), "TowerIcon", "Adv. Units Building", "Advanced units can be built adjacent to this.", 
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 5 }                              
                //{ MineralType.Azure, 2 }
            }));

        public static Blueprint Refinery = new Blueprint("Refinery", typeof(Refinery), "MinerIcon", "Refinery", "",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 3 },
                { MineralType.Silver, 1 }
            }));

        /*
         * Ship Parts
         * */

        private bool asLastPriority;

        public static Blueprint StationShipFrame = new Blueprint("StationShipProps", typeof(ShipProps), "ShipIcon", "Ship Frame", "Required for Ship Components",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Copper, 5 }
            }),
            (LayoutCoordinate layoutCoordinate) => {
                if(ResearchSingleton.sharedInstance.GetBuildingCount(typeof(ShipProps)) > 0) {
                    return false;
                }

                LayoutCoordinate[] adjCoordinates = layoutCoordinate.AdjacentCoordinates();
                if (adjCoordinates.Length != 4) {
                    return false;
                }

                MapGenerator mapGenerator = Script.Get<MapGenerator>();
                foreach(LayoutCoordinate coordinate in adjCoordinates) {
                    if(!mapGenerator.GetTerrainAt(coordinate).buildable) {
                        return false;
                    }
                }

                return true;                
            },
            "All Four Coordinates around Frame must be Buildable.",
            true); // As last priority

        public static Blueprint StationShip = new Blueprint("StationShip", typeof(StationShip), "ShipIcon", "Ship Frame", "Escape...",
            new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 4 },                
                { MineralType.Gold, 3 },                
                { MineralType.Azure, 2 }
            }),
            (LayoutCoordinate layoutCoordinate) => {
                BuildingManager buildingManager = Script.Get<BuildingManager>();

                Building thisCoordinate = buildingManager.buildlingAtLocation(layoutCoordinate);
                if(
                    thisCoordinate != null && thisCoordinate.GetType() == typeof(ShipProps) &&
                    buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipThrusters)) &&
                    buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipTelemetry)) &&
                    buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipMachining)) &&
                    buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipReactor))
                    ) {
                    return true;
                }

                return false;
            },
            "Must be build on a Frame with all four Components built.",
            true); // As last priority

        public static Blueprint Thrusters = new Blueprint("ShipThrusters", typeof(ShipThrusters), "ShipIcon", "Ship Thrusters", "Once built, ALL units MOVE 50% faster.",
           new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 4 },
                { MineralType.Gold, 3 },
                { MineralType.Azure, 2 }
           }),           
            (LayoutCoordinate layoutCoordinate) => {
                if(ResearchSingleton.sharedInstance.GetBuildingCount(typeof(ShipThrusters)) > 0) {
                    return false;
                }

                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipProps));
            },
            "Only 1.\nBuild Adjacent to " + Building.Blueprint.StationShipFrame.label,
            true); // As last priority

        public static Blueprint Reactor = new Blueprint("ShipReactor", typeof(ShipReactor), "ShipIcon", "Ship Reactor", "Once built, ALL units get duration +60s.",
           new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 4 },
                { MineralType.Gold, 3 },
                { MineralType.Azure, 2 }
           }),
            (LayoutCoordinate layoutCoordinate) => {
                if(ResearchSingleton.sharedInstance.GetBuildingCount(typeof(ShipReactor)) > 0) {
                    return false;
                }

                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipProps));
            },
            "Only 1.\nBuild Adjacent to " + Building.Blueprint.StationShipFrame.label,
            true); // As last priority

        public static Blueprint Machining = new Blueprint("ShipMachining", typeof(ShipMachining), "ShipIcon", "Ship Machining", "Once built, ALL units ACT 50% faster.",
           new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 4 },
                { MineralType.Gold, 3 },
                { MineralType.Azure, 2 }
           }),
            (LayoutCoordinate layoutCoordinate) => {
                if(ResearchSingleton.sharedInstance.GetBuildingCount(typeof(ShipMachining)) > 0) {
                    return false;
                }

                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipProps));
            },
            "Only 1.\nBuild Adjacent to " + Building.Blueprint.StationShipFrame.label,
            true); // As last priority

        public static Blueprint Telemerty = new Blueprint("ShipTelemetry", typeof(ShipTelemetry), "ShipIcon", "Ship Telemetry", "Once built, Tower vision increased 3->5.",
           new BlueprintCost(new Dictionary<MineralType, int>(){
                { MineralType.Silver, 4 },
                { MineralType.Gold, 3 },
                { MineralType.Azure, 2 }
           }),
            (LayoutCoordinate layoutCoordinate) => {
                if(ResearchSingleton.sharedInstance.GetBuildingCount(typeof(ShipTelemetry)) > 0) {
                    return false;
                }

                return Script.Get<BuildingManager>().IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipProps));
            },
            "Only 1.\nBuild Adjacent to " + Building.Blueprint.StationShipFrame.label,
            true); // As last priority

        /*
         * Constructors
         * */

        public Blueprint(string fileName, Type type, string iconName, string label, string description, BlueprintCost cost, bool asLastPriority = false) : 
            base(folder+fileName, type, iconName, label, description, cost) {

            // We cannot build any buildings next to the ship frame
            requirementsMet = (LayoutCoordinate layoutCoordinate) => {
                BuildingManager buildingManager = Script.Get<BuildingManager>();
                Building thisCoordinate = buildingManager.buildlingAtLocation(layoutCoordinate);
                
                return !buildingManager.IsLayoutCoordinateAdjacentToBuilding(layoutCoordinate, typeof(ShipProps), false) || (thisCoordinate != null);
            };

            requirementsNotMetString = "Cannot Build Adjacent to Ship Frame";

            this.asLastPriority = asLastPriority;
        }

        public Blueprint(string fileName, Type type, string iconName, string label, string description,  BlueprintCost cost, Func<LayoutCoordinate, bool> requirementsMet, string notMetString, bool asLastPriority = false) :
            base(folder + fileName, type, iconName, label, description, cost, requirementsMet, notMetString) { this.asLastPriority = asLastPriority; }

        public override GameObject ConstructAt(LayoutCoordinate layoutCoordinate) {
            BuildingManager buildingManager = Script.Get<BuildingManager>();

            Building building = UnityEngine.Object.Instantiate(resource) as Building;
            buildingManager.BuildAt(building, layoutCoordinate, cost, asLastPriority);

            return building.gameObject;
        }
    }

    public static Blueprint[] Blueprints() {
        return new Blueprint[] { Blueprint.Tower, Blueprint.Refinery, Blueprint.StationShip };
    }
}
