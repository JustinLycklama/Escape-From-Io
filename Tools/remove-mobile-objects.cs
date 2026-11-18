// Editor eval (#9): remove mobile-only objects before their code is deleted.
// - GameScene: the crosshair Image referenced by PlayerBehaviour.crosshairImage (joystick auto-select reticle)
// - PanControlsPanel prefab: the FixedJoystick child (keeps the WASD tutorial hint)
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Text;

var sb = new StringBuilder();

var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
var player = UnityEngine.Object.FindAnyObjectByType<PlayerBehaviour>(FindObjectsInactive.Include);
var so = new SerializedObject(player);
var crosshair = so.FindProperty("crosshairImage").objectReferenceValue as Component;
if (crosshair != null) {
    sb.AppendLine($"removing crosshair object '{crosshair.gameObject.name}'");
    UnityEngine.Object.DestroyImmediate(crosshair.gameObject);
    EditorSceneManager.MarkSceneDirty(scene);
    EditorSceneManager.SaveScene(scene);
} else {
    sb.AppendLine("no crosshair referenced");
}

const string PREFAB = "Assets/UI/Modular Panels/PanControlsPanel.prefab";
var root = PrefabUtility.LoadPrefabContents(PREFAB);
foreach (var j in root.GetComponentsInChildren<Joystick>(true)) {
    sb.AppendLine($"removing joystick object '{j.gameObject.name}' from prefab");
    UnityEngine.Object.DestroyImmediate(j.gameObject);
}
PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
PrefabUtility.UnloadPrefabContents(root);

return sb.ToString();
