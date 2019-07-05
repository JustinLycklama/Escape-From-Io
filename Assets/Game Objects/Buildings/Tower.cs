using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : Building {
    //protected override int requiredOre => 3;

    public MeshRenderer baseRenderer;
    public MeshRenderer topRenderer;

    // Declaration
    Shader originalShader;
    Shader buildableShader;

    Dictionary<MeshRenderer, bool> revertDictionary = new Dictionary<MeshRenderer, bool>();

    private void Start() {
        buildableShader = Shader.Find("Custom/Buildable");
        originalShader = baseRenderer.material.shader;

        baseRenderer.material.shader = buildableShader;
        topRenderer.material.shader = buildableShader;

        foreach(MeshRenderer renderer in new MeshRenderer[] {baseRenderer, topRenderer} ) {
            revertDictionary[renderer] = false;
        }
    }

    protected override void UpdateCompletionPercent(float percent) {
        if (percent <= 0.5 ) {
            baseRenderer.material.SetFloat("percentComplete", Mathf.InverseLerp(0, 0.5f, percent));
        } else {
            topRenderer.material.SetFloat("percentComplete", Mathf.InverseLerp(0.5f, 1, percent));
        }

        if (percent > 0.5 && revertDictionary[baseRenderer] == false) {
            baseRenderer.material.SetFloat("percentComplete", 1);
            baseRenderer.material.shader = originalShader;
        }

        if (percent == 1 && revertDictionary[topRenderer] == false) {
            topRenderer.material.shader = originalShader;
        }
    }
}
