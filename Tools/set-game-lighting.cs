// Editor eval (#27): GameScene light rig. A higher, warm sun; a cool fill light from the opposite side (no shadows,
// vertex/SH, so it costs nothing per pixel); and a sky/equator/ground (Trilight) ambient instead of one flat colour.
// Slopes facing away from the sun used to get only the flat ambient and read as black. Safe to re-run.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

var sun = GameObject.Find("Directional Light").GetComponent<Light>();
sun.lightmapBakeType = LightmapBakeType.Realtime;   // was Baked, with no baked lighting in the project
sun.transform.rotation = Quaternion.Euler(60, 330, 0);
sun.intensity = 1.0f;
sun.color = new Color(1f, 0.96f, 0.9f);
EditorUtility.SetDirty(sun); EditorUtility.SetDirty(sun.transform);

var fillGo = GameObject.Find("Fill Light");
if (fillGo == null) {
    fillGo = new GameObject("Fill Light");
    fillGo.transform.SetParent(sun.transform.parent, false);
    fillGo.AddComponent<Light>();
}
var fill = fillGo.GetComponent<Light>();
fill.type = LightType.Directional;
fill.lightmapBakeType = LightmapBakeType.Realtime;
fill.renderMode = LightRenderMode.ForceVertex;
fill.shadows = LightShadows.None;
fill.transform.rotation = Quaternion.Euler(30, 150, 0);
fill.intensity = 0.75f;
fill.color = new Color(0.72f, 0.8f, 1f);
EditorUtility.SetDirty(fill); EditorUtility.SetDirty(fillGo.transform);

RenderSettings.ambientMode = AmbientMode.Trilight;
RenderSettings.ambientSkyColor = new Color(0.70f, 0.74f, 0.84f);
RenderSettings.ambientEquatorColor = new Color(0.50f, 0.52f, 0.58f);
RenderSettings.ambientGroundColor = new Color(0.30f, 0.29f, 0.31f);

EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
return $"sun {sun.transform.eulerAngles} I={sun.intensity}; fill {fill.transform.eulerAngles} I={fill.intensity}; ambient={RenderSettings.ambientMode}";
