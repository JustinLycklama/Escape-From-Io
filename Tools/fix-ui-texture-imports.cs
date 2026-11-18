// Editor eval (readability): import settings for all first-party UI sprite textures under Assets/UI/Assets and Assets/Resources/UI.
// Most UI art is authored large and drawn small; without mipmaps that aliases ("low-res squished"), and DXT adds blockiness.
// Sets: mipmaps on, trilinear, uncompressed. Skips textures already set. Safe to re-run.
using UnityEditor;
using UnityEngine;
using System.Linq;

int changed = 0, total = 0;
foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI/Assets", "Assets/Resources/UI" })) {
    var path = AssetDatabase.GUIDToAssetPath(guid);
    if (!(AssetImporter.GetAtPath(path) is TextureImporter imp) || imp.textureType != TextureImporterType.Sprite) continue;
    total++;
    if (imp.mipmapEnabled && imp.filterMode == FilterMode.Trilinear && imp.textureCompression == TextureImporterCompression.Uncompressed) continue;
    imp.mipmapEnabled = true;
    imp.filterMode = FilterMode.Trilinear;
    imp.textureCompression = TextureImporterCompression.Uncompressed;
    imp.SaveAndReimport();
    changed++;
}
return $"UI sprite textures: {total} found, {changed} updated (mipmaps, trilinear, uncompressed)";
