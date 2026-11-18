// Editor eval (readability experiment on the robot cards):
// 1) Textures drawn far smaller than their source: enable mipmaps + trilinear, drop compression (crisp heavy downscaling).
// 2) Small 9-slice frames: thin the borders with Image.pixelsPerUnitMultiplier so corners don't overlap.
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Linq;

var sb = new StringBuilder();
var spriteNames = new[] { "oil-drill", "stock", "toolbox", "conveyor-belt", "lock", "arrows",
                          "sci-fi button02_normal", "sci-fi button03_normal", "sci-fi button04_normal", "Btn_Menu1_n", "loadingbar01_bg" };
foreach (var name in spriteNames) {
    var guid = AssetDatabase.FindAssets($"{name} t:Texture2D", new[] { "Assets/UI" })
        .FirstOrDefault(g => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)).Trim() == name);
    if (guid == null) { sb.AppendLine($"not found: {name}"); continue; }
    var path = AssetDatabase.GUIDToAssetPath(guid);
    var imp = (TextureImporter)AssetImporter.GetAtPath(path);
    imp.mipmapEnabled = true;
    imp.filterMode = FilterMode.Trilinear;
    imp.textureCompression = TextureImporterCompression.Uncompressed;
    imp.SaveAndReimport();
    sb.AppendLine($"mipmaps+trilinear+uncompressed: {path}");
}

const string PREFAB = "Assets/UI/Scale To Parent Cells/TaskAndUnitCell.prefab";
var root = PrefabUtility.LoadPrefabContents(PREFAB);
var card = root.transform.Find("Card");
void Mult(string child, float m) { var img = card.Find(child).GetComponent<Image>(); img.pixelsPerUnitMultiplier = m; sb.AppendLine($"{child}: pixelsPerUnitMultiplier={m}"); }
Mult("UnitsDetailsLayout", 2f);     // card background frame: 41px borders -> ~20px
Mult("TasksDetailsLayout", 3f);     // small right-hand frame: 41px -> ~14px, no longer overlaps at 77px tall
foreach (var bar in card.GetComponentsInChildren<PercentageBar>(true)) {
    var bg = bar.GetComponent<Image>(); bg.pixelsPerUnitMultiplier = 4f;   // horizontal bar art used vertically: thin its ends
}
sb.AppendLine("battery bar backgrounds: pixelsPerUnitMultiplier=4");
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);
return sb.ToString();
