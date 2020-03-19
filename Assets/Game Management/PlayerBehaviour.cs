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

    Rect UIRect;
    SettingsPanel settingsPanel;

    public static Color tintColor = new Color(0, 1, 1);

    public bool gamePaused { get; private set; }
    private bool lastPlayerPausedState = false;

    List<PlayerBehaviourUpdateDelegate> delegateList = new List<PlayerBehaviourUpdateDelegate>();

    private void Start() {
        RectTransform localRect = Tag.UIArea.GetGameObject().GetComponent<RectTransform>();

        Vector2 size = Vector2.Scale(localRect.rect.size, transform.lossyScale);
        UIRect = new Rect((Vector2)localRect.transform.position - (size * 0.5f), size);

        settingsPanel = Script.Get<SettingsPanel>();
    }

    // Update is called once per frame
    void Update() {
        CheckButtonInput();

        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if(!UIRect.Contains(mousePos)) {
            SelectGameObject();
        }
    }

    void CheckButtonInput() {

        var cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        var cameraRight = Camera.main.transform.right;
        cameraForward.y = 0f;
        cameraForward.Normalize();

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

    Ray lastRay;
    RaycastHit? lastHit;

    void SelectGameObject() {
        //Debug.DrawRay(lastRay.origin, lastRay.direction * 1000, Color.yellow);

        //if (lastHit != null) {
        //    Debug.DrawRay(lastHit.Value.point, Vector3.up * 1000, Color.red);
        //}       

        if(Input.GetMouseButtonDown(0)) {
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
                    if (!unit.initialized && building != null) {
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
    }


    /*
     * Public
     * */

    public void JumpCameraToTask(MasterGameTask masterGameTask) {
        GameTask firsGameTaskWithActionItem = null;
        
        foreach(GameTask gameTask in masterGameTask.childGameTasks) {
            if (gameTask.actionItem != null) {
                firsGameTaskWithActionItem = gameTask;
                break;
            }
        } 

        if (firsGameTaskWithActionItem != null) {
            JumpCameraToPosition(firsGameTaskWithActionItem.target.vector3);            
        }
    }

    public void JumpCameraToUnit(Unit unit) {
        if (unit != null) {
            JumpCameraToPosition(unit.transform.position);
        }
    }

    public void JumpCameraToPosition(Vector3 position) {
        Camera.main.transform.position = position + new Vector3(0, 250, -250);
    }

    /*
     * Menu paused state if used when a non player has requested a pause. 
     * When we return from our 'internal' paused state, we return to the state the player had, which could also be paused
     * */
    public void SetInternalPause(bool paused) {
        if (paused) {
            InternalUpdatePaused(true);
        } else {
            InternalUpdatePaused(lastPlayerPausedState);
        }
    }

    private void InternalUpdatePaused(bool paused) {
        bool oldPausedState = gamePaused;
        gamePaused = paused;

        if(oldPausedState != gamePaused) {
            NotifyAllPlayerBehaviourUpdate();
        }
    }

    public void SetPlayerPauseState(bool paused) {
        lastPlayerPausedState = paused;
        InternalUpdatePaused(paused);
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
