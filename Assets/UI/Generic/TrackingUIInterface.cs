using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TrackingUIInterface {
    Transform toFollow { get; set; }
    CanvasGroup canvasGroup { get; }
}

public static class TrackingUIInterfaceExtension {

    private static float outerCircleRadius = 400;
    private static float innerCircleRadius = 150;

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

        bool forceHideInterface = false;
        if(trackingInterface.canvasGroup.alpha > 0.1f && bb.y < aa.y) {
            forceHideInterface = true;
            //trackingInterface.canvasGroup.alpha = 0;
        }
        
        //else if(trackingInterface.canvasGroup.alpha == 0 && bb.y > aa.y) {
        //    trackingInterface.canvasGroup.alpha = 1;
        //}

        float magnitude = (aa - bb).magnitude;
        if (magnitude == 0) {
            magnitude = 0.001f;
        }

        behaviour.transform.localScale = Vector3.one * (1.0f / magnitude);

        Vector2 interfacePoint = new Vector2(screenPoint.x, screenPoint.y);

        // Canvas local coordinates are relative to its center, 
        // so we offset by half. We also discard the depth.
        screenPoint -= 0.5f * Vector3.one;
        screenPoint.z = 0;

        behaviour.transform.position = screenPoint;

        // Fade with distance to center
        Vector2 screenCenter = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);

        if (forceHideInterface) {
            if(trackingInterface.canvasGroup.alpha > 0) {
                trackingInterface.canvasGroup.alpha = 0;
            }

            return;
        }

        float distanceFromCenter = Vector2.Distance(screenCenter, interfacePoint);

        if(distanceFromCenter > outerCircleRadius) {
            trackingInterface.canvasGroup.alpha = 0;
        } else if (distanceFromCenter > innerCircleRadius) {
            trackingInterface.canvasGroup.alpha = 1.0f - Mathf.InverseLerp(innerCircleRadius, outerCircleRadius, distanceFromCenter);
        } else {
            trackingInterface.canvasGroup.alpha = 1;
        }        
    }
}
