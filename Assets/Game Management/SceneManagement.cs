using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface CanSceneChangeDelegate {
    bool CanWeSwitchScene();
}

public interface SceneChangeListener {
    void WillSwitchScene();
}

public class SceneManagement {

    public static SceneManagement sharedInstance = new SceneManagement();

    private const int MenuScene = 0;
    private const int GameScene = 1;

    /*
     * Title state is the defaut state on game load
     * GameFinish state is the state after the game is finished -> Show Leaderboard, option to submit if score achived
     * NewGame is the start of a new game
     * Tutorial is run the game in tutorial setup
     * */

    public enum State { Title, GameFinish, NewGame, Tutorial }

    public State state { get; private set; } = State.Title;
    public float? score { get; private set; } = null;

    private List<SceneChangeListener> delegateList = new List<SceneChangeListener>();

    // Experimental Dup of SceneChangeListener
    public UnityEngine.Events.UnityAction sceneChangeEvent;

    SceneManagement() {
        sceneChangeEvent = new UnityEngine.Events.UnityAction(SceneChange);
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public void ChangeScene(State state, Action<float> percentUpdated, Action complete, CanSceneChangeDelegate canChangeDelegate, float? score = null) {
        this.state = state;
        this.score = score;

        int scene = 0;

        switch(state) {
            case State.Title:
            case State.GameFinish:
                scene = MenuScene;
                break;
            case State.NewGame:
            case State.Tutorial:
                scene = GameScene;
                break;
        }

        SceneLoadHandler.sharedInstance.ChangeScene(scene, percentUpdated, complete, canChangeDelegate);
    }

    private void OnSceneUnloaded(Scene current) {
        Tag.ClearCache();
        Script.ClearCache();

        sceneChangeEvent.Invoke();
        NotifySceneListeners();
    }

    private void SceneChange() { }

    /*
     * SceneChangeListener Interface
     * */

    public void RegisterForSceneUpdates(SceneChangeListener listenerDelegate) {
        delegateList.Add(listenerDelegate);
    }

    public void EndSceneUpdates(SceneChangeListener listenerDelegate) {
        delegateList.Remove(listenerDelegate);
    }

    public void NotifySceneListeners() {
        foreach(SceneChangeListener listener in delegateList) {
            listener.WillSwitchScene();
        }
    }
}

public class SceneLoadHandler : MonoBehaviour {

    static SceneLoadHandler backingInstace;

    public static SceneLoadHandler sharedInstance {
        get {
            if (backingInstace == null) {
                GameObject backer = new GameObject();
                backingInstace = backer.AddComponent<SceneLoadHandler>();
            }

            return backingInstace;
        }
    }

    private const float LOAD_READY_PERCENTAGE = 0.9f;

    CanSceneChangeDelegate canChangeDelegate;

    public void ChangeScene(int scene, Action<float> percentUpdated, Action complete, CanSceneChangeDelegate canChangeDelegate) {
        this.canChangeDelegate = canChangeDelegate;
        StartCoroutine(LoadNewScene(scene, percentUpdated, complete));   
    }

    IEnumerator LoadNewScene(int scene, Action<float> percentUpdated, Action complete) {
        AsyncOperation async = SceneManager.LoadSceneAsync(scene);

        // disable scene activation while loading to prevent auto load
        async.allowSceneActivation = false;

        while(!async.isDone) {

            // Hold until our UI is ready for a switch
            if(async.progress >= LOAD_READY_PERCENTAGE) {            
                if(canChangeDelegate.CanWeSwitchScene()) {
                    yield return null;
                    async.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }
}

