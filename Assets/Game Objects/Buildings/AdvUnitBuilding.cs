using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvUnitBuilding : Building {
    public override string title => "Advanced Unit Building";
    public override float constructionModifierSpeed => 0.20f;

    public GameObject towerHead;

    private PlayerBehaviour playerBehaviour;

    protected override void UpdateCompletionPercent(float percent) {

    }

    protected override void CompleteBuilding() {
        playerBehaviour = Script.Get<PlayerBehaviour>();
        StartCoroutine(RotateHead());
    }

    IEnumerator RotateHead() {
        while(true) {
            yield return new WaitForSeconds(0.01f);

            if (playerBehaviour.gamePaused) {
                continue;
            }

            towerHead.transform.Rotate(Vector3.up, 2);
            
        }
    }
}
