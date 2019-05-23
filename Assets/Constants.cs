using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tag {
    private Tag(string value) { Value = value; }

    public string Value { get; set; }

    public static Tag Narrator { get { return new Tag("Narrator"); } }
    public static Tag Map { get { return new Tag("Map"); } }
    public static Tag MapGenerator { get { return new Tag("MapGenerator"); } }
    public static Tag AStar { get { return new Tag("AStar"); } }

    public GameObject GetGameObject() {
        return GameObject.FindGameObjectWithTag(this.Value);
    }
}