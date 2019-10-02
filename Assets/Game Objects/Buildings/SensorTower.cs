using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorTower : Building, TerrainUpdateDelegate {
    public override string title => "Azure Sensor";
    protected override float constructionModifierSpeed => 0.20f;

    private KeyValuePair<int, int>? closestLunarRock = null;

    Vector2 buildingPos;

    public GameObject towerObject;

    protected override void Start() {
        base.Start();

        Constants constants = Script.Get<Constants>();

        int startX = buildingLayoutCoordinate.mapContainer.mapX * constants.layoutMapWidth;
        int startY = buildingLayoutCoordinate.mapContainer.mapY * constants.layoutMapHeight;

        int xPos = startX + buildingLayoutCoordinate.x;
        int yPos = startY + buildingLayoutCoordinate.y;

        buildingPos = new Vector2(xPos, yPos);
    }

    private void OnDestroy() {
        Script.Get<MapsManager>().RemoveTerrainUpdateDelegate(this);
    }

    protected override void UpdateCompletionPercent(float percent) {
    }

    protected override void CompleteBuilding() {
        FindClosestAzure();
        RotateToNearestAzure();

        Script.Get<MapsManager>().AddTerrainUpdateDelegate(this);
    }

    // returns true if found NEW closest
    private bool FindClosestAzure() {
        MapGenerator mapGenerator = Script.Get<MapGenerator>();

        float distance = float.MaxValue;

        KeyValuePair<int, int>? closest = null;
        foreach(KeyValuePair<int, int> pair in mapGenerator.listOfLunarLocations) {
            float newDistance = Vector2.Distance(new Vector2(pair.Key, pair.Value), buildingPos);
            if (newDistance < distance) {
                distance = newDistance;
                closest = pair;
            }
        }

        if(closest == null && closestLunarRock == null) {
            return false;
        }

        bool closestUpdated = false;
        if((closest == null && closestLunarRock != null) || (closest != null && closestLunarRock == null) ||
            (closest.Value.Key != closestLunarRock.Value.Key || closest.Value.Value != closestLunarRock.Value.Value )) {
            closestUpdated = true;
        }

        closestLunarRock = closest;

        return closestUpdated;
    }


    private void ResetTower() {
        StartCoroutine(AttemptToRotate(new Vector3(0, 0, 0), 2));
    }

    private void RotateToNearestAzure() {
        if (closestLunarRock == null) {
            return;
        }

        KeyValuePair<int, int> azurePos = closestLunarRock.Value;

        int absX = Mathf.Abs(azurePos.Key - (int)buildingPos.x);
        int absY = Mathf.Abs(azurePos.Value - (int)buildingPos.y);

        int yRotation = 0;

        if (absX > absY) {
            if (azurePos.Key > buildingPos.x) {
                yRotation = 180;
            }
        } else {
            if(azurePos.Value < buildingPos.y) {
                yRotation = 90;
            } else {
                yRotation = -90;
            }
        }
        
        StartCoroutine(AttemptToRotate(new Vector3(0, yRotation, 90), 2));
    }

    IEnumerator AttemptToRotate(Vector3 angles, float duration) {
        yield return new WaitUntil(delegate {
            return rotating == false;
        });

        rotating = true;
        StartCoroutine(Rotate(angles, duration));
    }

    bool rotating;
    private IEnumerator Rotate(Vector3 angles, float duration) {

        Quaternion startRotation = towerObject.transform.rotation;
        Quaternion endRotation = Quaternion.Euler(angles);

        for(float t = 0; t < duration; t += Time.deltaTime) {
            towerObject.transform.rotation = Quaternion.Lerp(startRotation, endRotation, t / duration);
            yield return null;
        }

        towerObject.transform.rotation = endRotation;
        rotating = false;
    }

    /*
     * TerrainUpdateDelegate Interface
     * */

    public void NotifyTerrainUpdate(LayoutCoordinate layoutCoordinate) {
        bool newClosest = FindClosestAzure();

        if (newClosest) {
            ResetTower();
            RotateToNearestAzure();
        }
    }
}
