using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IntervalActionDelegate {
    void PerformIntervalAction();
}

// Allows delegates to sign up to perfom actions at equal spacings
public class IntervalActionPipeline : MonoBehaviour {
    //private static IntervalActionPipeline backingInstance;
    //public static IntervalActionPipeline sharedInstace {
    //    get {
    //        if (backingInstance == null) {
    //            backingInstance = new IntervalActionPipeline();
    //            backingInstance.StartDish();
    //        }

    //        return backingInstance;
    //    }
    //}

    private List<IntervalActionDelegate> delegates = new List<IntervalActionDelegate>();   

    private void Start() {
        StartCoroutine(DishActions());
    }

    public void Add(IntervalActionDelegate d) {
        delegates.Add(d);
    }

    public void Remove(IntervalActionDelegate d) {
        delegates.Remove(d);
    }

    IEnumerator DishActions() {
        while(true) {
            for(int i = 0; i < delegates.Count; i++) {
                delegates[i].PerformIntervalAction();
                yield return null;
            }

            yield return null;
        }
    }
}
