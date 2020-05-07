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

        rotating = true;

        Quaternion originalRotation = rotatingComponent.transform.rotation;

        float totalTurnDistance = 0;
        float degreesToTurn = (targetRotation.eulerAngles - originalRotation.eulerAngles).magnitude;

        bool turning = true;

        if(degreesToTurn < 5) {
            turning = false;
        }

        while(turning) {

            // Don't move on pause
            if(playerBehaviour.gamePaused) {
                yield return null;
                continue;
            }

            if(totalTurnDistance >= 1) {
                turning = false;
            } else {
                totalTurnDistance = Mathf.Clamp01(totalTurnDistance + ((Time.deltaTime * turnSpeed) / degreesToTurn * 180));
                rotatingComponent.transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, totalTurnDistance);
            }

            yield return null;
        }

        rotatingComponent.transform.rotation = targetRotation;
        rotating = false;

        completion?.Invoke();
    }
}
