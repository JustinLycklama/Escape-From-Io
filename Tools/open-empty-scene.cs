// Editor eval: switch the editor to a new empty scene (discarding nothing: game scenes are saved by the build scripts).
// Run before git operations that may rewrite *.unity files, so Unity doesn't block on a "reload scene?" prompt.
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
return "empty scene open";
