using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RotatingBuilding : Building {

    [SerializeField]
    protected GameObject rotatingComponent;

    protected PlayerBehaviour playerBehaviour;

    protected override void Start() {
        base.Start();

        playerBehaviour = Script.Get<PlayerBehaviour>();
    }

    protected Quaternion GetRotationQuad(Vector3 target) {
        target.y = transform.position.y;
        return Quaternion.LookRotation(target - transform.position);
    }

    protected void AttemptToRotate(Quaternion targetRotation, Action completion = null) {
        if (rotateRoutine != null) {
            StopCoroutine(rotateRoutine);
        }

        rotateRoutine = StartCoroutine(Rotate(targetRotation, completion));
    }

    protected bool rotating;
    protected float turnSpeed = 0.5f;
    Coroutine rotateRoutine;

    private IEnumerator Rotate(Quaternion targetRotation, Action completion = null) {

        while(true) {

            // Don't move on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            float degreesToTurn = (targetRotation.eulerAngles - rotatingComponent.transform.rotation.eulerAngles).magnitude;

            if(degreesToTurn > 1) {

                rotatingComponent.transform.rotation = Quaternion.RotateTowards(rotatingComponent.transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 90);

            } else {
                break;
            }

            yield return null;
        }

        completion?.Invoke();
    }
}
