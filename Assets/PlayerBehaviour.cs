using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    public MapPoint selectedPoint = new MapPoint(0, 0);

    float cameraMovementSpeed = 200;
    float cameraRotateSpeed = 100;

    Material mapMaterial;

    // Start is called before the first frame update
    void Start() {
        mapMaterial = Tag.Map.GetGameObject().GetComponent<Material>();
    }

    // Update is called once per frame
    void Update() {
        MoveCamera();
        SelectTile();        
    }

    void MoveCamera() {

        var cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        var cameraRight = Camera.main.transform.right;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if(Input.GetKey(KeyCode.D)) {
            Camera.main.transform.Translate(cameraRight * cameraMovementSpeed * Time.deltaTime, Space.World);
        }
        if(Input.GetKey(KeyCode.A)) {
            Camera.main.transform.Translate(-cameraRight * cameraMovementSpeed * Time.deltaTime, Space.World);
        }
        if(Input.GetKey(KeyCode.S)) {
            Camera.main.transform.Translate(-cameraForward * cameraMovementSpeed * Time.deltaTime, Space.World);
        }
        if(Input.GetKey(KeyCode.W)) {
            Camera.main.transform.Translate(cameraForward * cameraMovementSpeed * Time.deltaTime, Space.World);
        }

        if(Input.GetKey(KeyCode.RightArrow)) {
            Camera.main.transform.Rotate(new Vector3(0, cameraRotateSpeed * Time.deltaTime, 0), Space.World);
        }

        if(Input.GetKey(KeyCode.LeftArrow)) {
            Camera.main.transform.Rotate(new Vector3(0, -cameraRotateSpeed * Time.deltaTime, 0), Space.World);
        }
    }

        void SelectTile() {
        if(Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit)) {

                GameObject map = Tag.Map.GetGameObject();
                Transform mapObjectSpace = map.transform; //  display.meshRenderer.transform;

                Vector3 selectionPoint = hit.point;
                Vector3 selectedPointOnMap = mapObjectSpace.InverseTransformPoint(selectionPoint);

                //Debug.Log("Mouse Down Hit the following object: " + selectedPointOnMap);

                selectedPoint = new MapPoint(selectedPointOnMap.x + (MapGenerator.layoutMapWidth * MapGenerator.featuresPerLayoutPerAxis / 2f),
                    (-selectedPointOnMap.z) + (MapGenerator.layoutMapHeight * MapGenerator.featuresPerLayoutPerAxis / 2f));

                //mapGenerator.GenerateMap();

                mapMaterial.SetFloat("selectedXOffsetLow", selectedPoint.virtualX * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapWidth * MapGenerator.featuresPerLayoutPerAxis / 2f));
                mapMaterial.SetFloat("selectedXOffsetHigh", (selectedPoint.virtualX + 1) * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapWidth * MapGenerator.featuresPerLayoutPerAxis / 2f));

                mapMaterial.SetFloat("selectedYOffsetLow", selectedPoint.virtualY * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapHeight * MapGenerator.featuresPerLayoutPerAxis / 2f));
                mapMaterial.SetFloat("selectedYOffsetHigh", (selectedPoint.virtualY + 1) * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapHeight * MapGenerator.featuresPerLayoutPerAxis / 2f));
            }

        }
    }
}
