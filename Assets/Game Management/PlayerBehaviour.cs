using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface PlayerBehaviourUpdateDelegate {
    void PauseStateUpdated(bool paused);
}

public interface HotkeyDelegate {
    void HotKeyPressed(KeyCode key);
}

public class PlayerBehaviour : MonoBehaviour {
    float cameraMovementSpeed = 200;
    float cameraRotateSpeed = 100;

    SettingsPanel settingsPanel;

    public static Color tintColor = new Color(0, 1, 1);

    public bool gamePaused { get; private set; }
    private bool lastPlayerPausedState = false;

    List<PlayerBehaviourUpdateDelegate> delegateList = new List<PlayerBehaviourUpdateDelegate>();

    // Pan and Input Control
    private Vector2? initialTouchPosition;

    private Vector2? previousTouch = null;
    private Vector2? residualDirection = null;

    private float panFriction = 1.0f;

    [SerializeField]
    private float zoomOutMin = 1;
    [SerializeField]
    private float zoomOutMax = 8;

    // Object selection
    private Ray lastRay;
    private RaycastHit? lastHit;

    private void Start() {
        settingsPanel = Script.Get<SettingsPanel>();
    }

    // Update is called once per frame
    void Update() {

        if(Input.anyKey) {
            CheckHotkeyInput();
        }

        // We want as little as possible to be done here, first check to see if mouse down or up, and we are not over UI
        if((Input.GetMouseButtonUp(0) || Input.GetMouseButtonDown(0)) && EventSystem.current.IsPointerOverGameObject()) {
            return;
        }

        bool cameraAction = false;

        cameraAction |= CameraPan();
        cameraAction |= CameraZoom();

        if(!cameraAction && Input.GetMouseButtonUp(0)) {
            SelectGameObject();
        }
    }

    /*
     * Public
     * */

    public void JumpCameraToTask(MasterGameTask masterGameTask) {
        GameTask firsGameTaskWithActionItem = null;

        foreach(GameTask gameTask in masterGameTask.childGameTasks) {
            if(gameTask.actionItem != null) {
                firsGameTaskWithActionItem = gameTask;
                break;
            }
        }

        if(firsGameTaskWithActionItem != null) {
            JumpCameraToPosition(firsGameTaskWithActionItem.target.vector3);
        }
    }

    public void JumpCameraToUnit(Unit unit) {
        if(unit != null) {
            JumpCameraToPosition(unit.transform.position);
        }
    }

    public void JumpCameraToPosition(Vector3 position) {
        Camera.main.transform.position = position + new Vector3(0, 250, -250);

        //Camera.main.transform.position = position + new Vector3(0, 250, 250);
        //Camera.main.transform.Rotate(new Vector3(0, 180, 0), Space.World);
    }

    /*
     * Menu paused state if used when a non player has requested a pause. 
     * When we return from our 'internal' paused state, we return to the state the player had, which could also be paused
     * */
    public void SetInternalPause(bool paused) {
        if(paused) {
            EnactPauseUpdate(true);
        } else {
            EnactPauseUpdate(lastPlayerPausedState);
        }
    }

    public void SetPlayerPauseState(bool paused) {
        lastPlayerPausedState = paused;
        EnactPauseUpdate(paused);
    }


    /*
     * Helpers
     * */

    private void EnactPauseUpdate(bool paused) {
        bool oldPausedState = gamePaused;
        gamePaused = paused;

        if(oldPausedState != gamePaused) {
            NotifyAllPlayerBehaviourUpdate();
        }
    }

    /*
     * Input Checks
     * */

    private bool CameraPan() {

        bool hasPanned = false;

        if(initialTouchPosition.HasValue && previousTouch.HasValue) {
            hasPanned = Vector2.Distance(initialTouchPosition.Value, previousTouch.Value) > 1;
        }

        // Setup Camera Vectors
        var cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        var cameraRight = Camera.main.transform.right;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (Input.GetMouseButtonDown(0)) {
            initialTouchPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }

        if(Input.GetMouseButton(0) && Input.touchCount < 2) {

            Vector2 touch = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if (previousTouch.HasValue) {
                Vector2 direction = (touch - previousTouch.Value) * 0.05f;

                cameraRight.x *= direction.x;
                cameraForward.z *= direction.y;

                var finalDirection = cameraForward + cameraRight;
                Camera.main.transform.Translate(-finalDirection * cameraMovementSpeed * Time.deltaTime, Space.World);

                residualDirection = direction;
            }

            previousTouch = touch;
        } else {
            previousTouch = null;

            if (residualDirection.HasValue) {

                cameraRight.x *= residualDirection.Value.x;
                cameraForward.z *= residualDirection.Value.y;

                var finalDirection = cameraForward + cameraRight;
                Camera.main.transform.Translate(-finalDirection * cameraMovementSpeed * Time.deltaTime, Space.World);

                // Apply pan friction
                residualDirection -= residualDirection * 0.05f;
                if(Mathf.Abs(residualDirection.Value.x) < 0.01 && Mathf.Abs(residualDirection.Value.y) < 0.01) {
                    residualDirection = null;
                }
            }
        }

        return hasPanned;
    }

    private bool CameraZoom() {
        bool wasZooming = false;

        // Mouse Zoom
        // Don't care about wasZooming with mouse wheel, it will not interfere with object clicks
        zoom(Input.GetAxis("Mouse ScrollWheel") * 100f);

        // Finger Pinch Zoom
        if(Input.touchCount == 2) {
            wasZooming = true;

            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            zoom(difference * 0.01f);
        }

        return wasZooming;
    }

    private void zoom(float increment) {

        var cameraUp = Camera.main.transform.forward;
        cameraUp.Normalize();

        cameraUp *= increment;

        Camera.main.transform.Translate(cameraUp * cameraMovementSpeed * Time.deltaTime, Space.World);
    }

    private void SelectGameObject() {
        //Debug.DrawRay(lastRay.origin, lastRay.direction * 1000, Color.yellow);

        //if (lastHit != null) {
        //    Debug.DrawRay(lastHit.Value.point, Vector3.up * 1000, Color.red);
        //}       

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        lastRay = ray;

        if(Physics.Raycast(ray, out hit)) {
            lastHit = hit;

            GameObject objectHit = hit.transform.gameObject;
            SelectionManager selectionManager = Script.Get<SelectionManager>();

            Unit unit = objectHit.GetComponent<Unit>();
            Building building = objectHit.GetComponent<Building>();

            // Select a Terrain Selection
            if(objectHit.GetComponent<MapContainer>() != null) {
                BoxCollider boxCollider = hit.collider as BoxCollider;

                Vector3 worldPosition = boxCollider.transform.TransformPoint(boxCollider.center);

                MapCoordinate selectedCoordinate = MapCoordinate.FromWorldPosition(new WorldPosition(worldPosition));
                LayoutCoordinate coordinate = new LayoutCoordinate(selectedCoordinate);

                selectionManager.SelectTerrain(coordinate);
            }

            // Select a unit
            else if(unit != null) {
                if(!unit.initialized && building != null) {
                    selectionManager.SelectSelectable(building);
                } else {
                    selectionManager.SelectSelectable(unit);
                }
            } else if(building != null) {
                selectionManager.SelectSelectable(building);
            }

            // Deselect everything
            else {
                selectionManager.RemoveSelection();
            }
        }        
    }

    private void CheckHotkeyInput() {

        var cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        var cameraRight = Camera.main.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        // Camera Move
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

        // Camera Rotate
        if(Input.GetKey(KeyCode.RightArrow)) {
            Camera.main.transform.Rotate(new Vector3(0, cameraRotateSpeed * Time.deltaTime, 0), Space.World);
        }

        if(Input.GetKey(KeyCode.LeftArrow)) {
            Camera.main.transform.Rotate(new Vector3(0, -cameraRotateSpeed * Time.deltaTime, 0), Space.World);
        }

        // Hotkeys
        if(Input.GetKeyUp("escape")) {
            settingsPanel = Script.Get<SettingsPanel>();
        }

        if(Input.GetKeyUp(KeyCode.Space)) {
            SetPlayerPauseState(!gamePaused);
        }

        foreach(KeyCode vKey in System.Enum.GetValues(typeof(KeyCode))) {
            if(Input.GetKeyUp(vKey) && hotKeyListMap.ContainsKey(vKey)) {
                foreach(HotkeyDelegate keyDelegate in hotKeyListMap[vKey]) {
                    keyDelegate.HotKeyPressed(vKey);
                }
            }
        }
    }

    /*
     * HotkeyDelegate Interface
     * */

    private Dictionary<KeyCode, List<HotkeyDelegate>> hotKeyListMap = new Dictionary<KeyCode, List<HotkeyDelegate>>();

    public void AddHotKeyDelegate(KeyCode key, HotkeyDelegate keyDelegate) {
        if(!hotKeyListMap.ContainsKey(key)) {
            hotKeyListMap[key] = new List<HotkeyDelegate>();
        }

        List<HotkeyDelegate> list = hotKeyListMap[key];
        list.Add(keyDelegate);
    }
    
    public void RemoveHotKeyDelegate(HotkeyDelegate keyDelegate) {
        foreach(List<HotkeyDelegate> delegateList in hotKeyListMap.Values) {
            delegateList.Remove(keyDelegate);
        }
    }

    /*
     PlayerBehaviourUpdateDelegate Interface
    * */

    public void RegisterForPlayerBehaviourNotifications(PlayerBehaviourUpdateDelegate notificationDelegate) {
        delegateList.Add(notificationDelegate);

        notificationDelegate.PauseStateUpdated(gamePaused);
    }

    public void EndPlayerBehaviourNotifications(PlayerBehaviourUpdateDelegate notificationDelegate) {
        delegateList.Remove(notificationDelegate);
    }

    public void NotifyAllPlayerBehaviourUpdate() {
        foreach(PlayerBehaviourUpdateDelegate updateDelegate in delegateList) {
            updateDelegate.PauseStateUpdated(gamePaused);
        }
    }
}
