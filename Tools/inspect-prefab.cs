// Editor eval: dump a prefab's hierarchy with components, mesh/material names and missing references.
// Edit PREFABS below. Run: unity command script eval --file Tools/inspect-prefab.cs
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text;

var PREFABS = new[] { "Assets/Game Objects/Buildings/PointerArrow.prefab", "Assets/Resources/Buildings/StationShipProps.prefab" };
var sb = new StringBuilder();

void Dump(Transform t, int depth) {
    var comps = t.GetComponents<Component>().Select(c => {
        if (c == null) return "<missing script>";
        if (c is MeshFilter mf) return $"MeshFilter(mesh={(mf.sharedMesh ? mf.sharedMesh.name : "MISSING")})";
        if (c is MeshRenderer mr) return $"MeshRenderer(mats={string.Join("/", mr.sharedMaterials.Select(m => m ? m.name : "MISSING"))})";
        if (c is Transform) return null;
        return c.GetType().Name;
    }).Where(s => s != null);
    var missingPrefab = PrefabUtility.IsPrefabAssetMissing(t.gameObject) ? " [MISSING PREFAB]" : "";
    sb.AppendLine($"{new string(' ', depth * 2)}{t.name}{missingPrefab} pos={t.localPosition} rot={t.localEulerAngles} scale={t.localScale} :: {string.Join(", ", comps)}");
    foreach (Transform c in t) Dump(c, depth + 1);
}

foreach (var p in PREFABS) {
    sb.AppendLine($"== {p}");
    var root = PrefabUtility.LoadPrefabContents(p);
    Dump(root.transform, 0);
    PrefabUtility.UnloadPrefabContents(root);
}
return sb.ToString();
