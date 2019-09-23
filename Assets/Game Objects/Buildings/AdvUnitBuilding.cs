using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvUnitBuilding : Building {
    public override string title => "Advanced Unit Building";
    protected override float constructionModifierSpeed => 0.20f;

    public GameObject towerHead;

    protected override void UpdateCompletionPercent(float percent) {

    }

    protected override void CompleteBuilding() {
        StartCoroutine(RotateHead());
    }

    IEnumerator RotateHead() {
        while(true) {
            towerHead.transform.Rotate(Vector3.up, 2);
            yield return new WaitForSeconds(0.01f);
        }
    }
}
