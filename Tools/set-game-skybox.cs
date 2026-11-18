// Editor eval (#19): space skybox behind the map instead of a solid black clear, without changing how the terrain is lit.
// Ambient light used to come from the default procedural sky; pin it to that sky's current ambient colour so terrain
// brightness stays the same when the (much darker) space skybox goes in. Edit SKYBOX to try another. Safe to re-run.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

const string SKYBOX = "Assets/Textures/Skybox/2/Material/MilkyWay.mat";
var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

string before = $"ambientMode={RenderSettings.ambientMode} ambientSky={RenderSettings.ambientSkyColor} probe(L0)={RenderSettings.ambientProbe[0, 0]:0.###},{RenderSettings.ambientProbe[1, 0]:0.###},{RenderSettings.ambientProbe[2, 0]:0.###}";
if (RenderSettings.ambientMode == AmbientMode.Skybox) {
    // L0 SH coefficient * (0.886227 * 2) ≈ average ambient colour the old sky produced
    var p = RenderSettings.ambientProbe;
    float k = 0.886227f * 2f;
    var avg = new Color(p[0, 0] * k, p[1, 0] * k, p[2, 0] * k);
    RenderSettings.ambientMode = AmbientMode.Flat;
    RenderSettings.ambientLight = avg;
}

var sky = AssetDatabase.LoadAssetAtPath<Material>(SKYBOX);
RenderSettings.skybox = sky;
var cam = Camera.main;
cam.clearFlags = CameraClearFlags.Skybox;
EditorUtility.SetDirty(cam);

EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
return $"before: {before}\nafter: skybox={sky.name} ambientMode={RenderSettings.ambientMode} ambient={RenderSettings.ambientLight} camera clear={cam.clearFlags}";
