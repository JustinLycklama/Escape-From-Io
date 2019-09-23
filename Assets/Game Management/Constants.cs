using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants : MonoBehaviour {
    public int mapCountX;
    public int mapCountY;

    public int layoutMapWidth;
    public int layoutMapHeight;

    [Range(1, 10)]
    public int featuresPerLayoutPerAxis;

    [Range(1, 5)]
    public int nodesPerLayoutPerAxis;

    public int mapWidth { get { return layoutMapWidth * featuresPerLayoutPerAxis; } }
    public int mapHeight { get { return layoutMapHeight * featuresPerLayoutPerAxis; } }
}

public class Tag {
    private Tag(string value) { Value = value; }
    public string Value { get; set; }

    private static Dictionary<Tag, GameObject> objectCache = new Dictionary<Tag, GameObject>();    

    public static Tag Narrator { get { return new Tag("Narrator"); } }
    //public static Tag Map { get { return new Tag("Map"); } }
    public static Tag MapsManager { get { return new Tag("MapsManager"); } }
    public static Tag UnitManager { get { return new Tag("UnitManager"); } }
    public static Tag MapGenerator { get { return new Tag("MapGenerator"); } }
    public static Tag AStar { get { return new Tag("AStar"); } }
    public static Tag UIManager { get { return new Tag("UIManager"); } }
    public static Tag UIOverlayPanel { get { return new Tag("UIOverlayPanel"); } }
    public static Tag UIArea { get { return new Tag("UIArea"); } }
    public static Tag ResourceManager { get { return new Tag("ResourceManager"); } }
    public static Tag MiniMap { get { return new Tag("MiniMap"); } }
    public static Tag BuildingManager { get { return new Tag("BuildingManager"); } }
    public static Tag NotificationManager {  get { return new Tag("NotificationManager"); } }
    public static Tag FadePanel { get { return new Tag("FadePanel"); } }
    public static Tag SettingsPanel { get { return new Tag("SettingsPanel"); } }

    public GameObject GetGameObject() {
        GameObject cachedObject;
        if (objectCache.ContainsKey(this)) {
            cachedObject = objectCache[this];
        } else { 
            cachedObject = GameObject.FindGameObjectWithTag(this.Value);
            objectCache[this] = cachedObject;
        }

        return cachedObject;
    }

    public static void ClearCache() {
        objectCache.Clear();
    }

    public override int GetHashCode() {
        if(Value == null) return 0;
        return Value.GetHashCode();
    }

    public override bool Equals(object obj) {
        Tag other = obj as Tag;
        return other != null && other.Value == this.Value;
    }
}

public class Script {
    private Script(Tag tag, Type type) { this.tag = tag; this.type = type; }

    private Tag tag { get; set; }
    private Type type { get; set; }

    private static Dictionary<Type, Script> scriptCache = new Dictionary<Type, Script>();
    private static Dictionary<Script, Component> componentCache = new Dictionary<Script, Component>();

    public static Script Constants { get { return new Script(Tag.Narrator, typeof(Constants)); } }
    public static Script PlayerBehaviour { get { return new Script(Tag.Narrator, typeof(PlayerBehaviour)); } }
    //public static Script MapContainer { get { return new Script(Tag.Map, typeof(MapContainer)); } }
    public static Script MapsManager { get { return new Script(Tag.MapsManager, typeof(MapsManager)); } }
    public static Script UIManager { get { return new Script(Tag.UIManager, typeof(UIManager)); } }
    public static Script TaskQueue { get { return new Script(Tag.Narrator, typeof(TaskQueueManager)); } }
    public static Script MapGenerator { get { return new Script(Tag.MapGenerator, typeof(MapGenerator)); } }
    public static Script PathfindingGrid { get { return new Script(Tag.AStar, typeof(PathfindingGrid)); } }
    public static Script UIOverlayPanel { get { return new Script(Tag.UIOverlayPanel, typeof(RectTransform)); } }
    public static Script SelectionManager { get { return new Script(Tag.Narrator, typeof(SelectionManager)); } }
    public static Script UnitManager { get { return new Script(Tag.UnitManager, typeof(UnitManager)); } }
    public static Script TextureGenerator { get { return new Script(Tag.MapGenerator, typeof(TextureGenerator)); } }
    public static Script TerrainManager { get { return new Script(Tag.MapGenerator, typeof(TerrainManager)); } }
    public static Script ResourceManager { get { return new Script(Tag.ResourceManager, typeof(GameResourceManager)); } }
    public static Script MiniMap { get { return new Script(Tag.MiniMap, typeof(MiniMap)); } }
    public static Script BuildingManager { get { return new Script(Tag.BuildingManager, typeof(BuildingManager)); } }
    public static Script TimeManager { get { return new Script(Tag.Narrator, typeof(TimeManager)); } }
    public static Script NotificationManager { get { return new Script(Tag.NotificationManager, typeof(NotificationPanel)); } }
    public static Script FadePanel { get { return new Script(Tag.FadePanel, typeof(FadePanel)); } }
    public static Script SettingsPanel { get { return new Script(Tag.SettingsPanel, typeof(SettingsPanel)); } }
    public static Script IntervalActionPipeline { get { return new Script(Tag.Narrator, typeof(IntervalActionPipeline)); } }

    public static Script[] allScripts = new Script[] { Constants, PlayerBehaviour, MapsManager, UIManager, TaskQueue, MapGenerator, PathfindingGrid,
        SelectionManager, UnitManager, TextureGenerator, TerrainManager, ResourceManager, MiniMap, BuildingManager, TimeManager, NotificationManager, FadePanel,
        SettingsPanel, IntervalActionPipeline}; 

    public static T Get<T> () where T : Component {

        Type type = typeof(T);

        if(scriptCache.ContainsKey(type)) {
            return scriptCache[type].GetFromObject<T>();
        } else {
            foreach(Script script in allScripts) {
                if(script.type == type) {
                    scriptCache[type] = script;
                    return script.GetFromObject<T>();
                }
            }
        }

        return null;
    }

    public T GetFromObject<T>() where T : Component {

        Component component;
        if (componentCache.ContainsKey(this)) {
            component = componentCache[this];
        } else {
            GameObject gameObject = tag.GetGameObject();
            if (gameObject == null) { return null;  }

            component = gameObject.GetComponent<T>();
            componentCache[this] = component;
        }

        return (T)component;
    }

    public static void ClearCache() {
        scriptCache.Clear();
        componentCache.Clear();
    }

    public override int GetHashCode() {
        if(type == null) return 0;
        return type.GetHashCode();
    }

    public override bool Equals(object obj) {
        Script other = obj as Script;
        return other != null && other.type == this.type;
    }
}

public abstract class PrefabBlueprint {
    public PrefabBlueprint(string fileName, Type type) {
        this.fileName = fileName;
        this.type = type;

        this.resource = Resources.Load(fileName, type);
    }

    public string fileName { get; set; }
    public Type type { get; set; }

    public UnityEngine.Object resource;

    public UnityEngine.Object Instantiate() {
        return UnityEngine.Object.Instantiate(resource);
    }
}

public struct BlueprintCost {
    public Dictionary<MineralType, int> costMap;

    public BlueprintCost(Dictionary<MineralType, int> costMap) {
        this.costMap = costMap;
    }
}

public abstract class ConstructionBlueprint : PrefabBlueprint {

    public static string iconFolder = "UI/Icons/";

    public Sprite iconImage;

    public ConstructionBlueprint(string fileName, Type type, string iconName, string label, BlueprintCost cost) : base(fileName, type) {
        this.label = label;
        this.cost = cost;

        iconImage = Resources.Load<Sprite>(iconFolder + iconName);
    }

    public BlueprintCost cost;
    public string label;

    public abstract GameObject ConstructAt(LayoutCoordinate layoutCoordinate);
}