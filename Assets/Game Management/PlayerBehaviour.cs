using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public interface PlayerBehaviourUpdateDelegate {
    void PauseStateUpdated(bool paused);
}

public interface HotkeyDelegate {
    void HotKeyPressed(KeyCode key);
}

public class PlayerBehaviour : MonoBehaviour {
    //private const string UI_ELEMENT_TAG = "BlockingUIElement";

    private int UI_Layer;

    private const float maxCameraMovementSpeed = 200;
    private const float minCameraMovementSpeed = 50;

    private const float maxCameraAutoPanSpeed = 2;
    private const float minCameraAutoPanSpeed = 0.5f;

    private const float maxCameraAutoPanDistance = 200;
    private const float minCameraAutoPanDistance = 5;

    private const float cameraPanUIAlpha = 0.4f;

    [SerializeField]
    private AnimationCurve cameraAutoPanCurve;

    private const float maxCameraZoomPosition = 350;
    private const float minCameraZoomPosition = 150;

    private const float cameraRotateSpeed = 100;

    LayerMask terrainLayer;

    SettingsPanel settingsPanel;
    GraphicRaycaster graphicRaycaster;
    [SerializeField] private CanvasGroup ui_canvas;

    public bool gamePaused { get; private set; }
    private bool playerPausedState = false;
    private bool internalPauseState = false;

    List<PlayerBehaviourUpdateDelegate> delegateList = new List<PlayerBehaviourUpdateDelegate>();

    // Pan and Input Control
    private Vector2? initialTouchPosition;

    private Vector2? previousTouch = null;
    private Vector2? residualDirection = null;

    private float minJoystickThreshold = 0.35f;

    private float mouseDownTime = 0;
    private float secondsBeforeTouchPan = 0.075f;

    [SerializeField]
    private PanControlsPanel panContolPanel = null;

    [SerializeField]
    private Image crosshairImage = null;

    private bool isJoystickPanning = false;

    // Object selection
    private Ray lastRay;
    private RaycastHit? lastHit;

    bool mouseIsDownOverUI = false;

    private bool hotkeysEnabled;
    private bool joystickEnabled;

    private Vector3 cameraForward;
    private Vector3 cameraRight;

    private Rect? _boundary = null;
    private Rect boundary {
        get {
            if (_boundary != null) {
                return _boundary.Value;

            }

            MapsManager mapsManager = Script.Get<MapsManager>();
            Rect newBoundary = mapsManager.mapsBoundaries;

            newBoundary.x -= newBoundary.width / 2.0f;
            newBoundary.y -= newBoundary.height / 2.0f;

            newBoundary.width /= 2.0f;
            newBoundary.height /= 2.0f;

            newBoundary.x *= mapsManager.transform.localScale.x;
            newBoundary.y *= mapsManager.transform.localScale.z;

            newBoundary.width *= mapsManager.transform.localScale.x;
            newBoundary.height *= mapsManager.transform.localScale.z;

            newBoundary.y -= 200;
            newBoundary.height -= 200;

            _boundary = newBoundary;

            return newBoundary;
        }
        
    }

    private void Start() {
        settingsPanel = Script.Get<SettingsPanel>();
        graphicRaycaster = Script.Get<UIManager>().GetComponent<GraphicRaycaster>();

        UI_Layer = LayerMask.NameToLayer("UI");
        terrainLayer = ~LayerMask.NameToLayer("TerrainBoxCollider");

        hotkeysEnabled = !Application.isMobilePlatform;
        joystickEnabled = Application.isMobilePlatform;

        panContolPanel.SetJoystickEnabled(joystickEnabled);

        cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        cameraRight = Camera.main.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        if (joystickEnabled) {
            StartCoroutine(AutoClickWhileJoystick());
        } else {
            crosshairImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator AutoClickWhileJoystick() {

        Color fullColor = crosshairImage.color;

        Color fadedColor = fullColor;
        fadedColor.a = 0.25f;

        while(true) {
            if (!isJoystickPanning) {
                crosshairImage.color = fadedColor;
            }

            yield return new WaitUntil(() => { return isJoystickPanning; });

            crosshairImage.color = fullColor;

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            SelectGameObject(ray);

            yield return new WaitForSeconds(0.15f);
        }
    }

    void Update() {

        HotkeyInput();
        StaticPanInput();

        CameraApplyTouchPan();

        bool mouseDown = Input.GetMouseButtonDown(0);
        bool mouseUp = Input.GetMouseButtonUp(0);

        if (mouseIsDownOverUI) {
            if (mouseUp) {
                mouseIsDownOverUI = false;
            }

            //print("MouseStill Over UI");

            return;
        }

        // We want as little as possible to be done here, first check to see if mouse down or up, and we are not over UI
        if(mouseDown && IsOverUI(Input.mousePosition)) {
            mouseIsDownOverUI = true;
            //print("MouseDown Over UI");
            return;
        }

        bool cameraAction = false;

        //cameraAction |= CameraTouchPanInput();
        cameraAction |= CameraZoom();

        // If we are manually moving the camera while the auto pan is happening, stop auto pan
        if(cameraAction && panCameraRoutine != null) {
            StopCoroutine(panCameraRoutine);
        }

        if(!cameraAction && mouseUp) {

            //print("Mouse Button Up");
            //initialTouchPosition = null;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            SelectGameObject(ray);
        }
    }

    /*
     * Input Checks
     * */

    public bool IsOverUI(Vector3 inputPosition) {
        PointerEventData ped = new PointerEventData(null);
        ped.position = inputPosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(ped, results);

        return results.Where(res => res.gameObject.layer == UI_Layer).Count() > 0;
    }

    private void HotkeyInput() {
        if(!hotkeysEnabled /*|| !Input.anyKey*/) {
            print("SkipHotkeys");
            return;            
        }

        if(Input.GetKeyUp(KeyCode.Escape)) {
            settingsPanel = Script.Get<SettingsPanel>();
        }

        if(Input.GetKeyUp(KeyCode.Space)) {
            SetPlayerPauseState(!gamePaused);
        }

        foreach(KeyCode vKey in hotKeyListMap.Keys) {
            if (Input.GetKeyUp(vKey)) {
                foreach(HotkeyDelegate keyDelegate in hotKeyListMap[vKey]) {
                    keyDelegate.HotKeyPressed(vKey);
                }
            }
        }
    }

    List<Vector3> panVectors = new List<Vector3>();
    private void StaticPanInput() {

        panVectors.Clear();

        if (joystickEnabled) {

            float vertical = panContolPanel.joystick.Vertical;
            if(Mathf.Abs(vertical) > minJoystickThreshold) {
                vertical = Mathf.InverseLerp(minJoystickThreshold, 1, vertical);

                int sign = vertical > 0 ? 1 : -1;
                float verticalMovement = Mathf.Lerp(minCameraMovementSpeed, maxCameraMovementSpeed, Mathf.Abs(vertical));
                //Camera.main.transform.Translate(sign * cameraForward * verticalMovement * Time.deltaTime, Space.World);
                panVectors.Add(sign * cameraForward * verticalMovement);
            }

            float horizontal = panContolPanel.joystick.Horizontal;
            if(Mathf.Abs(horizontal) > minJoystickThreshold) {
                horizontal = Mathf.InverseLerp(minJoystickThreshold, 1, horizontal);


                int sign = horizontal > 0 ? 1 : -1;
                float horizontalMovement = Mathf.Lerp(minCameraMovementSpeed, maxCameraMovementSpeed, Mathf.Abs(horizontal));
                //Camera.main.transform.Translate(sign * cameraRight * horizontalMovement * Time.deltaTime, Space.World);
                panVectors.Add(sign * cameraRight * horizontalMovement);
            }

            isJoystickPanning = panVectors.Count > 0;

        } else {            
            if(Input.GetKey(KeyCode.D)) {
                panVectors.Add(cameraRight * maxCameraMovementSpeed);
            }
            if(Input.GetKey(KeyCode.A)) {
                panVectors.Add(-cameraRight * maxCameraMovementSpeed);
            }
            if(Input.GetKey(KeyCode.S)) {
                panVectors.Add(-cameraForward * maxCameraMovementSpeed);
            }
            if(Input.GetKey(KeyCode.W)) {
                panVectors.Add(cameraForward * maxCameraMovementSpeed);
            }
        }

        // If we are manually panning while auto pan is on, stop auto pan
        if(panVectors.Count > 0 && panCameraRoutine != null) {
            StopCoroutine(panCameraRoutine);
        }

        if (panVectors.Count > 0 && ui_canvas.alpha == 1) {
            ui_canvas.alpha = cameraPanUIAlpha;
        } else if (panVectors.Count == 0 && ui_canvas.alpha == cameraPanUIAlpha) {
            ui_canvas.alpha = 1;
        }

        foreach(Vector3 vector in panVectors) {
            PanByVector(vector);
        }

        //// Camera Rotate
        //if(Input.GetKey(KeyCode.RightArrow)) {
        //    Camera.main.transform.Rotate(new Vector3(0, cameraRotateSpeed * Time.deltaTime, 0), Space.World);
        //}

        //if(Input.GetKey(KeyCode.LeftArrow)) {
        //    Camera.main.transform.Rotate(new Vector3(0, -cameraRotateSpeed * Time.deltaTime, 0), Space.World);
        //}
    }

    private bool CameraTouchPanInput() {

        bool hasPanned = (mouseDownTime != 0 && Time.time > mouseDownTime + secondsBeforeTouchPan);

        //if(initialTouchPosition.HasValue && previousTouch.HasValue) {
        //    hasPanned = Vector2.Distance(initialTouchPosition.Value, previousTouch.Value) > 1;
        //}


        //if (residualDirection == null) {
        //    return false;
        //}

        if(Input.GetMouseButtonDown(0)) {
            mouseDownTime = Time.time;
            //return false;
        }

        else if(Input.GetMouseButtonUp(0)) {
            mouseDownTime = 0;

            initialTouchPosition = null;
            previousTouch = null;

            print("Mouse Button Up");
        }


        if(mouseDownTime != 0 && Input.touchCount < 2) {

            Vector2 touch = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if(initialTouchPosition == null) {
                initialTouchPosition = touch;
            }

            if (previousTouch.HasValue) {

                //if(Vector2.Distance(initialTouchPosition.Value, previousTouch.Value) < 10) {

                //    return false;
                //}


                residualDirection = (touch - previousTouch.Value) * 0.5f;
                //print("Dir: " + residualDirection);

            }

            previousTouch = touch;
        }

        //if (residualDirection == null) {
        //    print("direction Null");
        //    return false;
        //}



        return hasPanned;
    }

    private void CameraApplyTouchPan() {
        if(residualDirection == null) {            
            return;
        }

        var right = cameraRight;
        right.x *= residualDirection.Value.x;

        var forward = cameraForward;
        forward.z *= residualDirection.Value.y;

        var finalDirection = forward + right;
        //Camera.main.transform.Translate(-finalDirection * maxCameraMovementSpeed * Time.deltaTime, Space.World);

        print("Pan: " + residualDirection);
        PanByVector(-finalDirection * maxCameraMovementSpeed);

        // Apply pan friction
        residualDirection -= residualDirection * 0.05f;
        if(Mathf.Abs(residualDirection.Value.x) < 0.01 && Mathf.Abs(residualDirection.Value.y) < 0.01) {
            residualDirection = null;
        }
    }

    private bool CameraZoom() {
        bool wasZooming = false;

        // Mouse Zoom
        // Don't care about wasZooming with mouse wheel, it will not interfere with object clicks
        if (!IsOverUI(Input.mousePosition)) {
            Zoom(Input.GetAxis("Mouse ScrollWheel") * 100f);
        }        

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

            Zoom(difference * 0.1f);
        }

        return wasZooming;
    }

    // Enact motion on camera
    private void PanByVector(Vector3 vector) {

        Vector3 translation = vector * Time.deltaTime;
        Vector3 cameraAnticipatedLocation = Camera.main.transform.position + vector;

        if (IsPositionWithinBoundary(cameraAnticipatedLocation)) {
            Camera.main.transform.Translate(translation, Space.World);
        }
    }

    private void PanToLocation(Vector3 position) {

        if(IsPositionWithinBoundary(position)) {
            Camera.main.transform.position = position;
        }
    }

    private void Zoom(float increment) {

        var cameraUp = Camera.main.transform.forward;
        cameraUp.Normalize();

        cameraUp *= increment;

        Vector3 translation = cameraUp * maxCameraMovementSpeed * Time.deltaTime;
        Vector3 cameraAnticipatedLocation = Camera.main.transform.position + translation;

        if (IsPositionWithinBoundary(cameraAnticipatedLocation)) {
            Camera.main.transform.Translate(translation, Space.World);
        }
    }

    private bool IsPositionWithinBoundary(Vector3 position) {
        if (position.y > minCameraZoomPosition && position.y < maxCameraZoomPosition &&
            position.x > boundary.x && position.x < boundary.width &&
            position.z > boundary.y && position.z < boundary.height) {
            return true;
        }

        return false;
    }

    private void SelectGameObject(Ray ray) {
        //Debug.DrawRay(lastRay.origin, lastRay.direction * 1000, Color.yellow);

        //if (lastHit != null) {
        //    Debug.DrawRay(lastHit.Value.point, Vector3.up * 1000, Color.red);
        //}       

        RaycastHit hit;

        lastRay = ray;

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayer)) {
            lastHit = hit;

            GameObject objectHit = hit.transform.gameObject;
            SelectionManager selectionManager = Script.Get<SelectionManager>();

            //Unit unit = objectHit.GetComponent<Unit>();
            //Building building = objectHit.GetComponent<Building>();

            // Select a Terrain Selection
            if(objectHit.GetComponent<MapContainer>() != null) {
                BoxCollider boxCollider = hit.collider as BoxCollider;

                Vector3 worldPosition = boxCollider.transform.TransformPoint(boxCollider.center);

                MapCoordinate selectedCoordinate = MapCoordinate.FromWorldPosition(new WorldPosition(worldPosition));
                LayoutCoordinate coordinate = new LayoutCoordinate(selectedCoordinate);

                selectionManager.SelectTerrain(coordinate);
            }

            //// Select a unit
            //else if(unit != null) {
            //    if(!unit.initialized && building != null) {
            //        selectionManager.SelectSelectable(building);
            //    } else {
            //        selectionManager.SelectSelectable(unit);
            //    }
            //} else if(building != null) {
            //    selectionManager.SelectSelectable(building);
            //}

            // Deselect everything
            else {
                selectionManager.RemoveSelection();
            }
        }        
    }

    /*
     * Public
     * */

    public void PanCameraToTask(MasterGameTask masterGameTask) {
        GameTask firsGameTaskWithActionItem = null;

        foreach(GameTask gameTask in masterGameTask.childGameTasks) {
            if(gameTask.actionItem != null) {
                firsGameTaskWithActionItem = gameTask;
                break;
            }
        }

        if(firsGameTaskWithActionItem != null) {
            PanCameraToPosition(firsGameTaskWithActionItem.target.vector3);
        }
    }

    public void PanCameraToUnit(Unit unit) {
        if(unit != null) {
            PanCameraToPosition(unit.transform.position);
        }
    }

    //public void InitCameraPosition(Vector3 position) {
    //    PanCameraToPosition(Vector3.zero, false, 0.5f);
    //}


    private Vector3 GetCameraScenePositionForPan(Vector3 panToPosition, float? heightPercent = null) {
        float cameraHeight = Camera.main.transform.position.y;

        if(heightPercent.HasValue) {
            cameraHeight = Mathf.Lerp(minCameraZoomPosition, maxCameraZoomPosition, heightPercent.Value);
        }

        // Adjust camera ange here
        float pullbackPercent = Mathf.InverseLerp(minCameraZoomPosition, maxCameraZoomPosition, cameraHeight);
        float CameraPullBack = -Mathf.Lerp(minCameraZoomPosition, maxCameraZoomPosition, pullbackPercent);

        Vector3 newCameraPosition = panToPosition + new Vector3(0, 0, CameraPullBack);
        newCameraPosition.y = cameraHeight;

        return newCameraPosition;
    }

    public void PanCameraToPosition(Vector3 position, float? heightPercent = null, bool animated = true) {

        Vector3 newCameraPosition = GetCameraScenePositionForPan(position, heightPercent);

        if (animated) {
            if (panCameraRoutine != null) {
                StopCoroutine(panCameraRoutine);            
            }

            panCameraRoutine = StartCoroutine(PanCameraAnimated(newCameraPosition));
        } else {
            PanToLocation(newCameraPosition);
        }
    }

    private Coroutine panCameraRoutine;
    private IEnumerator PanCameraAnimated(Vector3 targetPosition) {

        Vector3 originalPosition = Camera.main.transform.position;
        float distance = Vector3.Distance(originalPosition, targetPosition);

        Vector3 direction = targetPosition - originalPosition;
        float totalDistance = 0;

        while(distance > minCameraAutoPanDistance) {
            float percentOfDistance = Mathf.InverseLerp(minCameraAutoPanDistance, maxCameraAutoPanDistance, distance);
            float cameraSpeed = Mathf.Lerp(minCameraAutoPanSpeed, maxCameraAutoPanSpeed, percentOfDistance);

            print("Speed " + cameraSpeed);

            totalDistance += cameraSpeed * Time.deltaTime;

            if (totalDistance > 1) {
                totalDistance = 1;
            }

            PanToLocation(Vector3.Lerp(originalPosition, targetPosition, totalDistance));

            yield return null;

            distance = Vector3.Distance(Camera.main.transform.position, targetPosition);
        }
    }

    /*
     * Menu paused state if used when a non player has requested a pause. 
     * When we return from our 'internal' paused state, we return to the state the player had, which could also be paused
     * */
    public void SetInternalPause(bool paused) {
        internalPauseState = paused;
        EnactPauseUpdate();
    }

    public void SetPlayerPauseState(bool paused) {
        playerPausedState = paused;
        EnactPauseUpdate();
    }

    /*
     * Helpers
     * */

    private void EnactPauseUpdate() {
        bool oldPausedState = gamePaused;
        gamePaused = playerPausedState || internalPauseState;

        if(oldPausedState != gamePaused) {
            NotifyAllPlayerBehaviourUpdate();
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
