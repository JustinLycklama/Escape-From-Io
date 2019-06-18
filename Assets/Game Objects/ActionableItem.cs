using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public interface ActionableItem {
//    float performAction(GameTask task, float rate, Unit unit);
//    void AssociateTask(GameTask task);
//    string description { get; }
//}


public abstract class ActionableItem : MonoBehaviour {
    string description;

    public abstract float performAction(GameTask task, float rate, Unit unit);
    public abstract void AssociateTask(GameTask task);
}
