// Editor eval (#6): build stand-in meshes from ProBuilder shapes for models whose .obj files were never committed,
// save them as plain Mesh assets, and assign them in the prefabs. Idempotent (overwrites the mesh assets).
//   - PointerArrow.prefab (Sensor Tower pointer): arrow along local +Y (the tower spins it about Y and aims that axis)
//   - StationShipProps.prefab child "Yellow_Station_Outpost": tiered octagonal tower. That child is disabled in the prefab
//     (the frame is meant to read as scaffolding), so this only fills the missing reference; it is not visible in game.
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Text;

const string MESH_DIR = "Assets/Game Objects/Buildings/StandInMeshes";
var sb = new StringBuilder();
if (!AssetDatabase.IsValidFolder(MESH_DIR)) AssetDatabase.CreateFolder("Assets/Game Objects/Buildings", "StandInMeshes");

// Generate a ProBuilder shape, bake it to a Mesh, and return it with its min-y moved to 'baseY' (centred on x/z)
Mesh Part(ProBuilderMesh pb, float baseY) {
    pb.ToMesh(); pb.Refresh();
    var mesh = Object.Instantiate(pb.GetComponent<MeshFilter>().sharedMesh);
    Object.DestroyImmediate(pb.gameObject);
    var b = mesh.bounds;
    var offset = new Vector3(-b.center.x, baseY - b.min.y, -b.center.z);
    var v = mesh.vertices;
    for (int i = 0; i < v.Length; i++) v[i] += offset;
    mesh.vertices = v;
    mesh.RecalculateBounds();
    return mesh;
}

Mesh Combine(string name, params Mesh[] parts) {
    var combine = new List<CombineInstance>();
    foreach (var p in parts) combine.Add(new CombineInstance { mesh = p, transform = Matrix4x4.identity });
    var mesh = new Mesh { name = name };
    mesh.CombineMeshes(combine.ToArray(), true, false);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    foreach (var p in parts) Object.DestroyImmediate(p);
    return mesh;
}

Mesh Save(Mesh mesh) {
    string path = $"{MESH_DIR}/{mesh.name}.asset";
    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if (existing != null) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing; }
    AssetDatabase.CreateAsset(mesh, path);
    return mesh;
}

// Arrow: 0.5 long along +Y (shaft 0.32, head 0.18). With the Sensor Tower's instance transforms it is ~22 world units,
// about the tower's width (tiles are 100 world units).
var arrow = Save(Combine("PointerArrow_StandIn",
    Part(ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.05f, 0.32f, 0), 0f),
    Part(ShapeGenerator.GenerateCone(PivotLocation.Center, 0.125f, 0.18f, 8), 0.32f)));

// Outpost: local units (prefab child is scaled 2.5): base r=9 h=8, mid r=5.5 h=6, cap cone r=5.5 h=4 (~45 world units tall)
var outpost = Save(Combine("StationOutpost_StandIn",
    Part(ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 9f, 8f, 0), 0f),
    Part(ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 5.5f, 6f, 0), 8f),
    Part(ShapeGenerator.GenerateCone(PivotLocation.Center, 5.5f, 4f, 8), 14f)));

// Assign
var arrowRoot = PrefabUtility.LoadPrefabContents("Assets/Game Objects/Buildings/PointerArrow.prefab");
var mf = arrowRoot.GetComponent<MeshFilter>();
if (mf == null) mf = arrowRoot.AddComponent<MeshFilter>(); // Unity fake-null: ?? does not work on components
mf.sharedMesh = arrow;
PrefabUtility.SaveAsPrefabAsset(arrowRoot, "Assets/Game Objects/Buildings/PointerArrow.prefab");
PrefabUtility.UnloadPrefabContents(arrowRoot);
sb.AppendLine($"PointerArrow: mesh {arrow.name} ({arrow.vertexCount} verts, bounds {arrow.bounds.size})");

var shipRoot = PrefabUtility.LoadPrefabContents("Assets/Resources/Buildings/StationShipProps.prefab");
var outpostGo = shipRoot.transform.Find("Yellow_Station_Outpost");
outpostGo.GetComponent<MeshFilter>().sharedMesh = outpost;
PrefabUtility.SaveAsPrefabAsset(shipRoot, "Assets/Resources/Buildings/StationShipProps.prefab");
PrefabUtility.UnloadPrefabContents(shipRoot);
sb.AppendLine($"StationShipProps/Yellow_Station_Outpost: mesh {outpost.name} ({outpost.vertexCount} verts, bounds {outpost.bounds.size})");

AssetDatabase.SaveAssets();
return sb.ToString();
