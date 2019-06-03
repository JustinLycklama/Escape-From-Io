using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ActionableItem {
    float performAction(GameTask task, float rate);
    string description { get; }
}

public class Building : MonoBehaviour, ActionableItem, Selectable {
    float percentComplete = 0;
    Color materialColor = Color.red;
    Color baseColor = Color.red;

    MeshRenderer buildingRenderer;

    StatusDelegate statusDelegate;

    private void Awake() {
        title = "Building #" + buildingCount;
        buildingCount++;
    }

    // Start is called before the first frame update
    void Start()
    {
        buildingRenderer = GetComponent<MeshRenderer>();
        buildingRenderer.material.shader = Shader.Find("Transparent/Diffuse");

        materialColor = baseColor;

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 3, transform.localScale.z);
    }

    // Update is called once per frame
    void Update() {
        materialColor.a = Mathf.Clamp(percentComplete, 0.10f, 1f);
        buildingRenderer.material.color = materialColor;
    }


    // MARK: Selectable Interface
    public void SetSelected(bool selected) {
        Color tintColor = selected ? Color.cyan : baseColor;
        materialColor = tintColor;
    }

    public void SetStatusDelegate(StatusDelegate statusDelegate) {
        this.statusDelegate = statusDelegate;
    }

    // Actionable Item

    // Returns the percent to completion the action is
    public float performAction(GameTask task, float rate) {
        switch(task.action) {
            case GameAction.Build:
                percentComplete += rate;

                if (percentComplete > 1) {
                    percentComplete = 1;
                }

                return percentComplete;
            case GameAction.Mine:
                break;
            default:
                throw new System.ArgumentException("Action is not handled", task.action.ToString());
        }

        return 100;
    }

    public static int buildingCount = 0;

    string title;
    public string description => title;



}
