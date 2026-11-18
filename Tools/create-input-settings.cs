// Editor eval (#8): project Input System settings so CLI-simulated input reaches the game in Play Mode.
// editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView (editor-only; builds unaffected).
// Idempotent. Run: unity command script eval --file Tools/create-input-settings.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

const string PATH = "Assets/InputSystem.inputsettings.asset";
var settings = AssetDatabase.LoadAssetAtPath<InputSettings>(PATH);
if (settings == null) {
    settings = ScriptableObject.CreateInstance<InputSettings>();
    AssetDatabase.CreateAsset(settings, PATH);
}

settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
EditorUtility.SetDirty(settings);
AssetDatabase.SaveAssets();

EditorBuildSettings.AddConfigObject("com.unity.input.settings", settings, true);
InputSystem.settings = settings;
// The config-object registration only reaches ProjectSettings/EditorBuildSettings.asset on a project save
EditorApplication.ExecuteMenuItem("File/Save Project");

return $"settings at {PATH}: editorInput={InputSystem.settings.editorInputBehaviorInPlayMode} background={InputSystem.settings.backgroundBehavior}";
