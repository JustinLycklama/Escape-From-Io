using System;
using UnityEngine;

public interface ActionableItem {
    float performAction(GameTask task, float rate);
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

    // Returns the percent to completion the action is
    public float performAction(GameTask task, float rate) {
        switch(task.action) {
            case GameAction.Build:
                percentComplete += rate;

                if (percentComplete > 1) {
                    percentComplete = 1;
                }

                return percentComplete;
            case GameAction.Mine:
                break;
            default:
                throw new System.ArgumentException("Action is not handled", task.action.ToString());
        }

        return 100;
    }

    public static int buildingCount = 0;

    string title;
    public string description => title;


    // STATIC

    public class Blueprint : PrefabBlueprint {
        public static Blueprint Tower = new Blueprint("Tower", "Light Tower", typeof(Tower));
        public static Blueprint Refinery = new Blueprint("Refinery", "Refinery", typeof(Refinery));

        public Blueprint(string fileName, string description, Type type) : base(fileName, description, type) {}
    }

    public static Blueprint[] Blueprints() {
        return new Blueprint[] { Blueprint.Tower, Blueprint.Refinery };
    }
}
