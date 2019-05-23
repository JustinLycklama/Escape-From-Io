using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    public LayoutCoordinate selectedLayoutTile = new LayoutCoordinate(0, 0);

    float cameraMovementSpeed = 200;
    float cameraRotateSpeed = 100;

    Material mapMaterial;

    // Start is called before the first frame update
    void Start() {
        mapMaterial = Tag.Map.GetGameObject().GetComponent<MeshRenderer>().material;
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

                MapCoordinate selectedCoordinate = new WorldPosition(hit.point).mapCoordinate;
                selectedLayoutTile = new LayoutCoordinate(selectedCoordinate);

                Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();

                //selectedLayoutTile = new LayoutCoordinate(selectedCoordinate.x + (MapGenerator.layoutMapWidth * MapGenerator.featuresPerLayoutPerAxis / 2f),
                //    (-selectedCoordinate.y) + (MapGenerator.layoutMapHeight * MapGenerator.featuresPerLayoutPerAxis / 2f));

                //Tag.MapGenerator.GetGameObject().GetComponent<MapGenerator>().GenerateMap();              

                mapMaterial.SetFloat("selectedXOffsetLow", selectedLayoutTile.x * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
                mapMaterial.SetFloat("selectedXOffsetHigh", (selectedLayoutTile.x + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

                mapMaterial.SetFloat("selectedYOffsetLow", selectedLayoutTile.y * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
                mapMaterial.SetFloat("selectedYOffsetHigh", (selectedLayoutTile.y + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
            }

        }
    }
}
