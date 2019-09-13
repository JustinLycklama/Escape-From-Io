using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOverlay : MonoBehaviour, TutorialEventListener {

    public Queue<TutorialScene> tutorialScenes;

    private PlayerBehaviour playerBehaviour;

    private void Start() {
        playerBehaviour = Script.Get<PlayerBehaviour>();

        ContinueTutorialQueue();
    }

    private void ContinueTutorialQueue() {
        TutorialScene scene = tutorialScenes.Dequeue();

        playerBehaviour.SetPauseState(scene.isPaused);
    }

    /*
     * TutorialEventListener Interface
     * */

    public void EventFired(TutorialEvent e) {
     
    }
}
