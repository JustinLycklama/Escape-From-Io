// Play Mode eval from TitleScene: start the Basic tutorial the way the title screen's tutorial button does.
// Note: eval wraps code in a class named "Script", which shadows the game's Script service locator, so use FindAnyObjectByType.
using UnityEngine;
using System.Reflection;

var panel = UnityEngine.Object.FindAnyObjectByType<TutorialPanel>(FindObjectsInactive.Include);
if (panel == null) return "TutorialPanel not found (are you in TitleScene play mode?)";

// The panel only allows the switch after its own fade; allow it immediately
typeof(TutorialPanel).GetField("ableToSwitchScene", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(panel, true);

TutorialManager.sharedInstance.tutorialType = TutorialType.Basic;
SceneManagement.sharedInstance.ChangeScene(SceneManagement.State.Tutorial, null, null, panel);
return "starting Basic tutorial";
