using System;
using UnityEngine;

public class Ore : MonoBehaviour
{
    public class Blueprint : PrefabBlueprint {
        public static Blueprint Basic = new Blueprint("Ore", "StandardOre", typeof(Ore));

        public Blueprint(string fileName, string description, Type type) : base(fileName, description, type) { }
    }
}
