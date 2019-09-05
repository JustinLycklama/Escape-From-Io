using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomShipGenerator : MonoBehaviour {

    public Transform startPos;
    public Transform endPos;

    public List<GameObject> demoShips;

    private System.Random random;
    private GameObject currentDemoShip;
    private bool currentRotate = false;

    private const float speed = 450f;
    private const float rotationSpeed = 0.25f;

    private List<bool> shouldRotate = new List<bool> { false, false, false, false, false, false, false, false, false, true, true, true, false };

    void Awake() {
        random = new System.Random(Guid.NewGuid().GetHashCode());
    }

    void Start() {
        startPos.transform.SetParent(transform, true);
        endPos.transform.SetParent(transform, true);

        foreach(GameObject gameObject in demoShips) {
            gameObject.transform.SetParent(transform, true);
        }

        StartCoroutine(CreateNewDisplayAfterSeconds(2));
    }

    IEnumerator CreateNewDisplayAfterSeconds (int seconds) {

        yield return new WaitForSeconds(seconds);

        int newShipIndex = random.Next(0, demoShips.Count - 1);
        currentDemoShip = demoShips[newShipIndex];
        currentRotate = shouldRotate[newShipIndex];

        currentDemoShip.transform.localPosition = startPos.localPosition;
        currentDemoShip.transform.LookAt(endPos);

        StartCoroutine(UpdateDisplayPosition());
    }

    IEnumerator UpdateDisplayPosition() {

        float lastDistance = float.MaxValue;

        while(true) {

            if(currentDemoShip == null) {
                yield break;
            }

            var distance = Vector3.forward * Time.deltaTime * speed;

            currentDemoShip.transform.Translate(distance, Space.Self);
            currentDemoShip.transform.Rotate(0, 0, rotationSpeed * (currentRotate ? 1 : 0));

            float newDistance = Vector3.Distance(currentDemoShip.transform.position, endPos.position);

            if(newDistance > lastDistance) {
                StartCoroutine(CreateNewDisplayAfterSeconds(2));
                yield break;
            }

            lastDistance = newDistance;
            yield return null;
        }
    }
}
