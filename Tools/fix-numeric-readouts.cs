// Editor eval (#14): numeric readout cells use DS-DIGI (numeric face) and a sane Best Fit cap.
// Best Fit up to 150 with Agency FB overflowed the dynamic font atlas and drew boxes.
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

var digi = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Assets/Fonts/DS-DIGI.TTF");
var sb = new System.Text.StringBuilder();
foreach (var p in new[] { "Assets/UI/Scale To Parent Cells/ImageValueCell.prefab", "Assets/UI/Modular Panels/ResourcesPanel.prefab" }) {
    var root = PrefabUtility.LoadPrefabContents(p);
    foreach (var t in root.GetComponentsInChildren<Text>(true)) {
        t.font = digi;
        t.resizeTextMaxSize = Mathf.Min(t.resizeTextMaxSize, 40);
        sb.AppendLine($"{p} :: {t.name} -> {t.font.name}, bestFit max {t.resizeTextMaxSize}");
    }
    PrefabUtility.SaveAsPrefabAsset(root, p);
    PrefabUtility.UnloadPrefabContents(root);
}
return sb.ToString();
