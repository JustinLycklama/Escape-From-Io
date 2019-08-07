using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Followable {
    Transform followTransform { get; }
    Transform lookAtTransform { get; }
}

public class CameraPanel : MonoBehaviour {
    public Camera followCamera;
    public GameObject cameraRenderPanel;

    private void Awake() {
        cameraRenderPanel.SetActive(false);
    }

    public void SetSelection(Selection selection) {

        Followable followable = null;

        if (selection != null) {
            followable = selection.selection as Followable;
        }

        if(followable != null && cameraRenderPanel.activeSelf == false) {
            cameraRenderPanel.SetActive(true);
        } else if (followable == null && cameraRenderPanel.activeSelf == true) {
            cameraRenderPanel.SetActive(false);
        }

        if(followable != null) {
            followCamera.transform.position = Vector3.zero;
            followCamera.transform.SetParent(followable.followTransform, false);

            RectTransform rectTransform = followCamera.GetComponent<RectTransform>();
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.Rotate(new Vector3(7.5f, 0, 0), Space.Self);
        }
    }
}
