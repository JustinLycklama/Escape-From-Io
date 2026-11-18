// Editor eval: list materials under a folder with their shader names and whether the shader supports UI stencil masking.
// Usage: unity command script eval --file Tools/list-material-shaders.cs   (edit FOLDER below)
using UnityEditor;
using UnityEngine;
using System.Linq;

const string FOLDER = "Assets/UI/SciFi UI Pings VFX/Files/Materials";
var lines = AssetDatabase.FindAssets("t:Material", new[] { FOLDER })
    .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
    .Select(m => $"{m.name}: shader='{m.shader.name}' hasStencil={m.HasProperty("_Stencil")} hasTint={m.HasProperty("_TintColor")}");
return string.Join("\n", lines);
