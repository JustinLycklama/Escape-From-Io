// Editor eval (#7): switch the SciFi UI Pings VFX materials from legacy particle shaders to the stencil-aware UI/Particles/* shaders.
// Idempotent. Keeps _MainTex/_TintColor (same property names). Run: unity command script eval --file Tools/swap-ui-particle-shaders.cs
using UnityEditor;
using UnityEngine;
using System.Linq;

const string FOLDER = "Assets/UI/SciFi UI Pings VFX/Files/Materials";
AssetDatabase.Refresh();

var additive = Shader.Find("UI/Particles/Additive");
var alpha = Shader.Find("UI/Particles/Alpha Blended");
if (additive == null || alpha == null) return "shaders not found (import pending?)";
if (ShaderUtil.ShaderHasError(additive) || ShaderUtil.ShaderHasError(alpha)) return "shader compile errors";

int swapped = 0;
foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { FOLDER })) {
    var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
    Shader target = mat.shader.name == "Legacy Shaders/Particles/Additive" ? additive
                  : mat.shader.name == "Legacy Shaders/Particles/Alpha Blended" ? alpha : null;
    if (target == null) continue;

    var tint = mat.GetColor("_TintColor");
    var tex = mat.GetTexture("_MainTex");
    var st = new Vector4(mat.mainTextureScale.x, mat.mainTextureScale.y, mat.mainTextureOffset.x, mat.mainTextureOffset.y);
    var queue = mat.renderQueue;

    mat.shader = target;
    mat.SetColor("_TintColor", tint);
    mat.SetTexture("_MainTex", tex);
    mat.mainTextureScale = new Vector2(st.x, st.y);
    mat.mainTextureOffset = new Vector2(st.z, st.w);
    mat.renderQueue = queue;
    EditorUtility.SetDirty(mat);
    swapped++;
}
AssetDatabase.SaveAssets();
return $"swapped {swapped} materials";
