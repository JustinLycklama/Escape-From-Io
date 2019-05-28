using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selection {

    public enum SelectionType {Terrain, Building, Unit}
    public SelectionType selectionType;

    // Terrain Properties
    public LayoutCoordinate coordinate;

    // Unit Properties
    Unit unit;

    private Selection() {}

    // Accessors
    public static Selection createTerrainSelection(LayoutCoordinate coordinate) {
        Selection selection = new Selection();
        selection.setTerrain(coordinate);

        return selection;
    }

    public void setTerrain(LayoutCoordinate coordinate) {
        selectionType = SelectionType.Terrain;
        this.coordinate = coordinate;

        // Select
        Constants constants = Tag.Narrator.GetGameObject().GetComponent<Constants>();              
        Material mapMaterial = Tag.Map.GetGameObject().GetComponent<MeshRenderer>().material;

        mapMaterial.SetFloat("selectedXOffsetLow", coordinate.x * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
        mapMaterial.SetFloat("selectedXOffsetHigh", (coordinate.x + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

        mapMaterial.SetFloat("selectedYOffsetLow", coordinate.y * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
        mapMaterial.SetFloat("selectedYOffsetHigh", (coordinate.y + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));

        mapMaterial.SetFloat("hasSelection", 1);

        // Set info
    }

    public static Selection createUnitSelection(Unit unit) {
        Selection selection = new Selection();
        selection.setUnit(unit);

        return selection;
    }

    public void setUnit(Unit unit) {
        selectionType = SelectionType.Unit;
        this.unit = unit;

        // Select
        unit.SetSelected(true);

        // Set info
    }

    // Mutators
    public void deselectCurrent() {
        if(selectionType == SelectionType.Terrain) {
            Material mapMaterial = Tag.Map.GetGameObject().GetComponent<MeshRenderer>().material;
            mapMaterial.SetFloat("hasSelection", 0);

        } else if(selectionType == SelectionType.Unit) {
            unit.SetSelected(false);
        }
    }

        // Properties
        public string Title() {
        if (selectionType == SelectionType.Terrain) {
            Map map = Script.MapContainer.GetFromObject<MapContainer>().getMap();

            return map.GetTerrainAt(coordinate).name;
        }
        else if (selectionType == SelectionType.Unit) {
            return "Unit";
        }

        return "";
    }

}

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
                    selection = Selection.createUnitSelection(unit);
                }
               
                Script.UIManager.GetFromObject<UIManager>().SetSelection(selection);

            }
        }
    }
}
