﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants : MonoBehaviour {
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

    public static Tag Narrator { get { return new Tag("Narrator"); } }
    public static Tag Map { get { return new Tag("Map"); } }
    public static Tag MapGenerator { get { return new Tag("MapGenerator"); } }
    public static Tag AStar { get { return new Tag("AStar"); } }
    public static Tag UIManager { get { return new Tag("UIManager"); } }

    public GameObject GetGameObject() {
        return GameObject.FindGameObjectWithTag(this.Value);
    }
}

public struct Script {
    private Script(Tag tag, Type type) { this.tag = tag; this.type = type; }

    private Tag tag { get; set; }
    private Type type { get; set; }

    public static Script Constants { get { return new Script(Tag.Narrator, typeof(Constants)); } }
    public static Script PlayerBehaviour { get { return new Script(Tag.Narrator, typeof(PlayerBehaviour)); } }
    public static Script MapContainer { get { return new Script(Tag.Map, typeof(MapContainer)); } }
    public static Script UIManager { get { return new Script(Tag.UIManager, typeof(UIManager)); } }
    public static Script TaskQueue { get { return new Script(Tag.Narrator, typeof(TaskQueue)); } }

    public static Script[] allScripts = new Script[] { Constants, PlayerBehaviour, MapContainer, UIManager, TaskQueue }; 

    public static T Get<T> () where T : class {

        foreach (Script script in allScripts) {
            if (script.type == typeof(T)) {
                return script.GetFromObject<T>();
            }
        }

        return null;
    }

    public T GetFromObject<T>() {
        GameObject gameObject = tag.GetGameObject();
        return gameObject.GetComponent<T>();
    }
}