using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    //public LayoutCoordinate selectedLayoutTile = new LayoutCoordinate(0, 0);

    float cameraMovementSpeed = 200;
    float cameraRotateSpeed = 100;

    Material mapMaterial;

    public Selection selection;

    // Start is called before the first frame update
    void Start() {
        mapMaterial = Tag.Map.GetGameObject().GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update() {
        MoveCamera();
        SelectTile();        
    }

    public 

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
                if (selection != null) {
                    selection.deselectCurrent();
                }

                GameObject objectHit = hit.transform.gameObject;

                // Select a Terrain Selection
                if(objectHit == Tag.Map.GetGameObject()) {
                    MapCoordinate selectedCoordinate = new WorldPosition(hit.point).mapCoordinate;
                    selection = Selection.createTerrainSelection(new LayoutCoordinate(selectedCoordinate));
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
