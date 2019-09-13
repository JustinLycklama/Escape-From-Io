using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TrackingUIInterface {
    Transform toFollow { get; set; }
    CanvasGroup canvasGroup { get; }
}

public static class TrackingUIInterfaceExtension {

    public static void UpdateTrackingPosition(this TrackingUIInterface trackingInterface) {
        if(trackingInterface.toFollow == null) {
            return;
        }

        // Translate the world position into viewport space.
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(trackingInterface.toFollow.position);

        // Used to scale up UI
        float sizeOnScreen = 10;

        // Get distance from screen to modify local scale as the camera moves away
        Vector3 b = new Vector3(screenPoint.x, screenPoint.y + sizeOnScreen, screenPoint.z);

        Vector3 aa = Camera.main.ScreenToWorldPoint(screenPoint);
        Vector3 bb = Camera.main.ScreenToWorldPoint(b);

        MonoBehaviour behaviour = trackingInterface as MonoBehaviour;

        //if(behaviour.gameObject.activeSelf == true && bb.y < aa.y) {
        //    //behaviour.gameObject.SetActive(false);
        //    trackingInterface.canvasGroup.alpha = 0;
        //} else if(behaviour.gameObject.activeSelf == false && bb.y > aa.y) {
        //    //behaviour.gameObject.SetActive(true);
        //    trackingInterface.canvasGroup.alpha = 1;
        //}

        if(trackingInterface.canvasGroup.alpha == 1 && bb.y < aa.y) {
            trackingInterface.canvasGroup.alpha = 0;
        } else if(trackingInterface.canvasGroup.alpha == 0 && bb.y > aa.y) {
            trackingInterface.canvasGroup.alpha = 1;
        }

        behaviour.transform.localScale = Vector3.one * (1.0f / (aa - bb).magnitude);

        // Canvas local coordinates are relative to its center, 
        // so we offset by half. We also discard the depth.
        screenPoint -= 0.5f * Vector3.one;
        screenPoint.z = 0;

        behaviour.transform.position = screenPoint;

        if (trackingInterface is MonoBehaviour) {
 
        }
    }
}
