using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : Building {

    public override string title => "Tower";
    protected override float constructionModifierSpeed => 0.20f;

    public override BuildingEffectStatus BuildingStatusEffects() {
        return BuildingEffectStatus.Light;
    }

    public override int BuildingStatusRange() {
        return 3 + ResearchSingleton.sharedInstance.visionRadiusAddiiton;
    }

    //protected override int requiredOre => 3;

    //public MeshRenderer baseRenderer;
    //public MeshRenderer topRenderer;

    //Dictionary<MeshRenderer, bool> revertDictionary = new Dictionary<MeshRenderer, bool>();

    protected override void UpdateCompletionPercent(float percent) {
        //if (percent <= 0.5 ) {
        //    baseRenderer.material.SetFloat("percentComplete", Mathf.InverseLerp(0, 0.5f, percent));
        //} else {
        //    topRenderer.material.SetFloat("percentComplete", Mathf.InverseLerp(0.5f, 1, percent));
        //}

        //if (percent > 0.5 && revertDictionary[baseRenderer] == false) {
        //    baseRenderer.material.SetFloat("percentComplete", 1);
        //    baseRenderer.material.shader = originalShader;
        //}

        //if (percent == 1 && revertDictionary[topRenderer] == false) {
        //    topRenderer.material.shader = originalShader;
        //}
    }

    protected override void CompleteBuilding() {
        //baseRenderer.material.SetFloat("percentComplete", 1);
        //topRenderer.material.SetFloat("percentComplete", 1);

        //baseRenderer.material.shader = originalShader;
        //topRenderer.material.shader = originalShader;
    }
}
