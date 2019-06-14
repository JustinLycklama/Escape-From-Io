using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {
    float cameraMovementSpeed = 200;
    float cameraRotateSpeed = 100;  

    public Selection selection;

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

    Ray lastRay;
    RaycastHit? lastHit;

    void SelectTile() {
        Debug.DrawRay(lastRay.origin, lastRay.direction * 1000, Color.yellow);

        if (lastHit != null) {
            Debug.DrawRay(lastHit.Value.point, Vector3.up * 1000, Color.red);
        }
        
        if(Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            lastRay = ray;

            if(Physics.Raycast(ray, out hit)) {
                lastHit = hit;

                if (selection != null) {
                    selection.deselectCurrent();
                }

                GameObject objectHit = hit.transform.gameObject;

                // Select a Terrain Selection
                if(objectHit.GetComponent<MapContainer>() != null) {
                    BoxCollider boxCollider = hit.collider as BoxCollider;

                    Vector3 worldPosition = boxCollider.transform.TransformPoint(boxCollider.center);

                    MapCoordinate selectedCoordinate = MapCoordinate.FromWorldPosition(new WorldPosition(worldPosition));
                    LayoutCoordinate coordinate = new LayoutCoordinate(selectedCoordinate);
                    selection = Selection.createTerrainSelection(coordinate);
                }

                // Select a unit
                Unit unit = objectHit.GetComponent<Unit>();
                if (unit != null) {
                    selection = Selection.createSelectableSelection(unit);
                }

                // Select a Building
                Building building = objectHit.GetComponent<Building>();
                if(building != null) {
                    selection = Selection.createSelectableSelection(building);
                }

                Script.UIManager.GetFromObject<UIManager>().SetSelection(selection);

            }
        }
    }
}
