using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActionableItem : MonoBehaviour {
    public abstract float performAction(GameAction action, float rate);
}

public class Building : ActionableItem {
    float percentComplete = 0;
    Color materialColor;

    MeshRenderer renderer;

    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("Transparent/Diffuse");

        materialColor = renderer.material.color;
    }

    // Returns the percent to completion the action is
    public override float performAction(GameAction action, float rate) {
        switch(action) {
            case GameAction.Build:
                percentComplete += rate;

                if (percentComplete > 1) {
                    percentComplete = 1;
                }

                return percentComplete;
            case GameAction.Destroy:
                break;
            case GameAction.Mine:
                break;
            default:
                throw new System.ArgumentException("Action is not handled", action.ToString());
        }

        return 100;
    }


    // Update is called once per frame
    void Update()
    {
        materialColor.a = Mathf.Clamp(percentComplete, 0.10f, 1f);
        renderer.material.color = materialColor;
    }
}
