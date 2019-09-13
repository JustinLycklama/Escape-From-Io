using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialEvent {

}


public interface TutorialEventListener {
    void EventFired(TutorialEvent e);
}


public class TutorialSingleton : MonoBehaviour {
    public static TutorialSingleton sharedInstance;

    private Dictionary<TutorialEvent, TutorialEventListener> eventMap = new Dictionary<TutorialEvent, TutorialEventListener>();

    public void Fire(TutorialEvent e) {
        NotifyEvent(e);
    }

    /*
     * TutorialEventListener Implementation
     * */

    public void ListenForEvent(TutorialEventListener updateDelegate, TutorialEvent tutorialEvent) {
        eventMap[tutorialEvent] = updateDelegate;

    }

    public void RemoveListener(TutorialEventListener updateDelegate) {
        foreach(TutorialEvent e in eventMap.Keys) {
            if (eventMap[e] == updateDelegate) {
                eventMap.Remove(e);
                return;
            }
        }
    }

    private void NotifyEvent(TutorialEvent e) {
        if (eventMap.ContainsKey(e)) {
            eventMap[e].EventFired(e);
;       }
    }
}
