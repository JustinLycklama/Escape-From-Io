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
    public static Tag MapGenerator { get { return new Tag("MapGenerator"); } }
    public static Tag AStar { get { return new Tag("AStar"); } }
    public static Tag UIManager { get { return new Tag("UIManager"); } }
    public static Tag UIOverlayPanel { get { return new Tag("UIOverlayPanel"); } }
    public static Tag UIArea { get { return new Tag("UIArea"); } }

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
}

public class Script {
    private Script(Tag tag, Type type) { this.tag = tag; this.type = type; }

    private Tag tag { get; set; }
    private Type type { get; set; }

    private static Dictionary<Script, Component> objectCache = new Dictionary<Script, Component>();

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
    public static Script UnitManager { get { return new Script(Tag.Narrator, typeof(UnitManager)); } }


    public static Script[] allScripts = new Script[] { Constants, PlayerBehaviour, MapsManager, UIManager, TaskQueue, MapGenerator, PathfindingGrid, SelectionManager, UnitManager }; 

    public static T Get<T> () where T : Component {

        foreach (Script script in allScripts) {
            if (script.type == typeof(T)) {
                return script.GetFromObject<T>();
            }
        }

        return null;
    }

    public T GetFromObject<T>() where T : Component {
        GameObject gameObject = tag.GetGameObject();

        Component component;
        if (objectCache.ContainsKey(this)) {
            component = objectCache[this];
        } else {  
            component = gameObject.GetComponent<T>();
            objectCache[this] = component;
        }

        return (T)component;
    }
}

public abstract class PrefabBlueprint {
    public PrefabBlueprint(string fileName, string description, Type type) {
        this.fileName = fileName;
        this.description = description;
        this.type = type;

        this.resource = Resources.Load(fileName, type);
    }

    public string fileName { get; set; }
    public string description { get; set; }
    public Type type { get; set; }

    public UnityEngine.Object resource;

    public UnityEngine.Object Instantiate() {
        return UnityEngine.Object.Instantiate(resource);
    }
}