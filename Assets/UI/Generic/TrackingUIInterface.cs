using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface TrackingUIInterface {
    Transform followPosition { get; set; }
    Transform followingObject { get; set; }
    CanvasGroup canvasGroup { get; }
}

public static class TrackingUIInterfaceExtension {

    private static float outerCircleRadius = 100;
    private static float innerCircleRadius = 50;

    public static void UpdateTrackingPosition(this TrackingUIInterface trackingInterface) {
        if(trackingInterface.followPosition == null) {
            return;
        }

        // Translate the world position into viewport space.
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(trackingInterface.followPosition.position);

        // Used to scale up UI
        float sizeOnScreen = 10;

        // Get distance from screen to modify local scale as the camera moves away
        Vector3 b = new Vector3(screenPoint.x, screenPoint.y + sizeOnScreen, screenPoint.z);

        Vector3 aa = Camera.main.ScreenToWorldPoint(screenPoint);
        Vector3 bb = Camera.main.ScreenToWorldPoint(b);

        MonoBehaviour behaviour = trackingInterface as MonoBehaviour;

        bool forceHideInterface = false;
        if(trackingInterface.canvasGroup.alpha > 0.1f && bb.y < aa.y) {
            forceHideInterface = true;
        }
        
        float magnitude = (aa - bb).magnitude;
        if (magnitude == 0) {
            magnitude = 0.001f;
        }

        behaviour.transform.localScale = Vector3.one * (1.0f / magnitude);

        // Canvas local coordinates are relative to its center, 
        // so we offset by half. We also discard the depth.
        screenPoint -= 0.5f * Vector3.one;
        screenPoint.z = 0;

        behaviour.transform.position = screenPoint;

        // Fade with distance to center
        Vector3 followObjectPoint = Camera.main.WorldToScreenPoint(trackingInterface.followingObject.position);
        Vector2 interfacePoint = new Vector2(followObjectPoint.x, followObjectPoint.y);

        Vector2 viewPoint = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
            
        if (forceHideInterface) {
            if(trackingInterface.canvasGroup.alpha > 0) {
                trackingInterface.canvasGroup.alpha = 0;
            }

            return;
        }

        float distanceFromCenter = Vector2.Distance(viewPoint, interfacePoint);

        if(distanceFromCenter > outerCircleRadius) {
            trackingInterface.canvasGroup.alpha = 0;
        } else if (distanceFromCenter > innerCircleRadius) {
            trackingInterface.canvasGroup.alpha = 1.0f - Mathf.InverseLerp(innerCircleRadius, outerCircleRadius, distanceFromCenter);
        } else {
            trackingInterface.canvasGroup.alpha = 1;
        }        
    }
}
